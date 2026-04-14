using Imate.API.Business.Interfaces.ExternalServices;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using System.Diagnostics;

namespace Imate.API.Business.Services.ExternalServices
{
    /// <summary>
    /// Azure Speech Recognition Service Implementation
    /// Supports Vietnamese and English speech-to-text conversion
    /// </summary>
    public class AzureSpeechRecognitionService : IAzureSpeechRecognitionService
    {
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly string? _endpoint;

        public AzureSpeechRecognitionService(IConfiguration configuration)
        {
            _subscriptionKey = configuration["AzureSpeech:SubscriptionKey"] 
                ?? throw new InvalidOperationException("AzureSpeech:SubscriptionKey is not configured.");
            _region = configuration["AzureSpeech:Region"] 
                ?? throw new InvalidOperationException("AzureSpeech:Region is not configured.");
            _endpoint = configuration["AzureSpeech:Endpoint"];
        }

        /// <summary>
        /// Converts audio data (WAV format) to text
        /// </summary>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string language = "vi-VN")
        {
            using var audioStream = new MemoryStream(audioData);
            return await RecognizeSpeechFromStreamAsync(audioStream, language);
        }

        /// <summary>
        /// Converts audio stream to text using Azure Speech SDK
        /// </summary>
        public async Task<string> RecognizeSpeechFromStreamAsync(Stream audioStream, string language = "vi-VN")
        {
            PushAudioInputStream? pushStream = null;
            AudioConfig? audioConfig = null;
            SpeechRecognizer? recognizer = null;

            try
            {
                // Create Speech Config
                var speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
                speechConfig.SpeechRecognitionLanguage = language;
                
                // Enable detailed logging for debugging (optional - can be removed in production)
                speechConfig.SetProperty(PropertyId.Speech_LogFilename, Path.Combine(Path.GetTempPath(), $"speech_log_{Guid.NewGuid()}.txt"));

                // Set endpoint if provided (note: EndpointId is for custom models, not endpoint URL)
                // Remove endpoint setting if it's causing issues
                if (!string.IsNullOrEmpty(_endpoint) && !string.IsNullOrWhiteSpace(_endpoint))
                {
                    // Only set if it's a custom endpoint ID, not the base URL
                    if (!_endpoint.StartsWith("http"))
                    {
                        speechConfig.EndpointId = _endpoint;
                    }
                }

                // Set property to increase timeout for initial silence
                speechConfig.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "5000");
                speechConfig.SetProperty(PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "500");

                // Read audio data from input stream
                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();

                // Convert audio to WAV PCM format (supports MP3, WAV, etc.)
                WavFileInfo wavInfo;
                try
                {
                    wavInfo = ConvertToWavPcm(audioBytes);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to process audio file: {ex.Message}. Supported formats: MP3, WAV (PCM).", ex);
                }

                // Validate audio format (Azure Speech requires: 16kHz, 16-bit, mono)
                if (wavInfo.SampleRate != 16000 || wavInfo.BitsPerSample != 16 || wavInfo.Channels != 1)
                {
                    // If format is not correct, resample/convert it
                    wavInfo = ResampleAudio(wavInfo, 16000, 16, 1);
                }

                // Validate PCM data exists and has content
                if (wavInfo.PcmData == null || wavInfo.PcmData.Length == 0)
                {
                    throw new InvalidOperationException("No audio data found in audio file.");
                }

                // Validate PCM data size (at least 0.5 seconds of audio at 16kHz, 16-bit, mono)
                // 0.5 seconds = 16000 samples/second * 2 bytes/sample * 0.5 seconds = 16000 bytes
                const int minimumAudioSize = 16000; // ~0.5 seconds
                if (wavInfo.PcmData.Length < minimumAudioSize)
                {
                    var duration = (double)wavInfo.PcmData.Length / (16000 * 2); // samples / (sample_rate * bytes_per_sample)
                    throw new InvalidOperationException(
                        $"Audio file is too short ({duration:F2} seconds). " +
                        $"Minimum duration: 0.5 seconds. " +
                        $"Please ensure your audio file contains at least 0.5 seconds of speech.");
                }

                // Validate final format
                if (wavInfo.SampleRate != 16000 || wavInfo.BitsPerSample != 16 || wavInfo.Channels != 1)
                {
                    throw new InvalidOperationException(
                        $"Audio format validation failed. " +
                        $"Required: 16kHz, 16-bit, mono. " +
                        $"Found: {wavInfo.SampleRate}Hz, {wavInfo.BitsPerSample}-bit, {wavInfo.Channels} channel(s).");
                }

                // Create push stream for audio data (16kHz, 16bit, mono)
                var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
                pushStream = AudioInputStream.CreatePushStream(audioFormat);

                // Push raw PCM audio data to the push stream
                // Note: We push the entire PCM data array at once
                pushStream.Write(wavInfo.PcmData);
                pushStream.Close(); // Close after writing to signal end of stream

                // Create audio config from push stream (must be done after writing and closing)
                audioConfig = AudioConfig.FromStreamInput(pushStream);

                // Create speech recognizer
                recognizer = new SpeechRecognizer(speechConfig, audioConfig);

                // Perform recognition (RecognizeOnceAsync for single-shot recognition)
                var result = await recognizer.RecognizeOnceAsync();

                // Handle result
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    // Check if text is empty or just whitespace
                    if (string.IsNullOrWhiteSpace(result.Text))
                    {
                        throw new InvalidOperationException(
                            "Speech recognition completed but no text was recognized. " +
                            "Please ensure your audio file contains clear speech and is not too noisy.");
                    }
                    return result.Text;
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    var noMatchDetails = NoMatchDetails.FromResult(result);
                    var reasonDescription = GetNoMatchReasonDescription(noMatchDetails.Reason);
                    throw new InvalidOperationException(
                        $"No speech could be recognized. Reason: {noMatchDetails.Reason} ({reasonDescription}). " +
                        $"Please ensure your audio file: " +
                        $"1) Contains clear speech, " +
                        $"2) Is not too noisy, " +
                        $"3) Is at least 0.5 seconds long, " +
                        $"4) Has sufficient volume/amplitude.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    throw new InvalidOperationException($"Speech recognition canceled. Reason: {cancellation.Reason}, Error: {cancellation.ErrorDetails}");
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected recognition result reason: {result.Reason}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during speech recognition: {ex.Message}", ex);
            }
            finally
            {
                // Clean up resources
                recognizer?.Dispose();
                audioConfig?.Dispose();
                pushStream?.Dispose();
            }
        }

        /// <summary>
        /// WAV file information structure
        /// </summary>
        private class WavFileInfo
        {
            public int SampleRate { get; set; }
            public int BitsPerSample { get; set; }
            public int Channels { get; set; }
            public byte[]? PcmData { get; set; }
        }

        /// <summary>
        /// Converts audio file (MP3, WAV, WebM, etc.) to WAV PCM format
        /// </summary>
        private WavFileInfo ConvertToWavPcm(byte[] audioData)
        {
            // Try to detect format by checking file signatures
            if (IsWavFile(audioData))
            {
                return ParseWavFile(audioData);
            }
            else if (IsMp3File(audioData))
            {
                return ConvertMp3ToWavPcm(audioData);
            }
            else if (IsWebMFile(audioData))
            {
                return ConvertWebMToWavWithFFmpeg(audioData);
            }
            else
            {
                // Try to read with NAudio as fallback (supports various formats via Media Foundation on Windows)
                // Note: NAudio relies on system codecs (Media Foundation on Windows) for WebM/Opus
                return ConvertAudioWithNAudio(audioData);
            }
        }

        /// <summary>
        /// Checks if audio data is a WAV file
        /// </summary>
        private bool IsWavFile(byte[] audioData)
        {
            if (audioData.Length < 12) return false;
            string riffHeader = System.Text.Encoding.ASCII.GetString(audioData, 0, 4);
            string waveHeader = System.Text.Encoding.ASCII.GetString(audioData, 8, 4);
            return riffHeader == "RIFF" && waveHeader == "WAVE";
        }

        /// <summary>
        /// Checks if audio data is an MP3 file
        /// </summary>
        private bool IsMp3File(byte[] audioData)
        {
            if (audioData.Length < 3) return false;
            // MP3 files can start with ID3 tag or MP3 sync bytes
            // ID3v2: starts with "ID3"
            if (audioData.Length >= 3 && System.Text.Encoding.ASCII.GetString(audioData, 0, 3) == "ID3")
                return true;
            // MP3 sync bytes: 0xFF 0xFB or 0xFF 0xF3
            if (audioData.Length >= 2 && audioData[0] == 0xFF && (audioData[1] & 0xE0) == 0xE0)
                return true;
            return false;
        }

        /// <summary>
        /// Checks if audio data is a WebM file (used by MediaRecorder API)
        /// </summary>
        private bool IsWebMFile(byte[] audioData)
        {
            if (audioData.Length < 4) return false;
            
            // WebM files start with EBML header (0x1A 0x45 0xDF 0xA3)
            if (audioData[0] == 0x1A && audioData[1] == 0x45 && audioData[2] == 0xDF && audioData[3] == 0xA3)
                return true;
            
            return false;
        }

        /// <summary>
        /// Converts MP3 file to WAV PCM using NAudio
        /// </summary>
        private WavFileInfo ConvertMp3ToWavPcm(byte[] mp3Data)
        {
            // Mp3FileReader requires file path, not Stream
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".mp3");
            
            try
            {
                // Write MP3 data to temp file
                File.WriteAllBytes(tempFilePath, mp3Data);

                using var reader = new Mp3FileReader(tempFilePath);
                
                // Resample to 16kHz, mono, 16-bit
                using var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 16, 1));
                
                using var outputStream = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(outputStream, resampler);
                
                var wavBytes = outputStream.ToArray();
                return ParseWavFile(wavBytes);
            }
            finally
            {
                // Delete temp file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch
                {
                    // Ignore errors when deleting temp file
                }
            }
        }

        /// <summary>
        /// Converts audio file using NAudio (supports various formats)
        /// </summary>
        private WavFileInfo ConvertAudioWithNAudio(byte[] audioData)
        {
            // AudioFileReader requires file path, not Stream
            // So we need to write to temp file first
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");
            WaveStream? reader = null;
            
            try
            {
                // Write audio data to temp file
                File.WriteAllBytes(tempFilePath, audioData);

                // Try to create appropriate reader based on format
                try
                {
                    // AudioFileReader supports many formats (MP3, WAV, M4A, etc.)
                    reader = new AudioFileReader(tempFilePath);
                }
                catch
                {
                    // If AudioFileReader fails, try MP3 directly
                    try
                    {
                        reader = new Mp3FileReader(tempFilePath);
                    }
                    catch
                    {
                        throw new InvalidOperationException(
                            "Unsupported audio format. Supported formats: MP3, WAV (PCM), WebM (Opus), M4A. " +
                            "Note: WebM support requires Media Foundation codecs on Windows.");
                    }
                }

                if (reader == null)
                {
                    throw new InvalidOperationException("Failed to read audio file.");
                }

                // Resample to 16kHz, mono, 16-bit
                using var resampler = new MediaFoundationResampler(reader, new WaveFormat(16000, 16, 1));
                
                using var outputStream = new MemoryStream();
                WaveFileWriter.WriteWavFileToStream(outputStream, resampler);
                
                var wavBytes = outputStream.ToArray();
                return ParseWavFile(wavBytes);
            }
            finally
            {
                // Clean up
                reader?.Dispose();
                
                // Delete temp file
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch
                {
                    // Ignore errors when deleting temp file
                }
            }
        }

        /// <summary>
        /// Resamples audio to target format (16kHz, 16-bit, mono)
        /// </summary>
        private WavFileInfo ResampleAudio(WavFileInfo sourceInfo, int targetSampleRate, int targetBitsPerSample, int targetChannels)
        {
            if (sourceInfo.PcmData == null || sourceInfo.PcmData.Length == 0)
            {
                throw new InvalidOperationException("No audio data to resample.");
            }

            // Create WAV file from PCM data
            var sourceFormat = new WaveFormat(sourceInfo.SampleRate, sourceInfo.BitsPerSample, sourceInfo.Channels);
            using var sourceStream = new RawSourceWaveStream(sourceInfo.PcmData, 0, sourceInfo.PcmData.Length, sourceFormat);
            
            // Resample to target format
            var targetFormat = new WaveFormat(targetSampleRate, targetBitsPerSample, targetChannels);
            var resampler = new MediaFoundationResampler(sourceStream, targetFormat);
            
            using var outputStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(outputStream, resampler);
            
            var wavBytes = outputStream.ToArray();
            return ParseWavFile(wavBytes);
        }

        /// <summary>
        /// Parses WAV file and extracts format information and PCM data
        /// </summary>
        private WavFileInfo ParseWavFile(byte[] wavData)
        {
            // Check if it's a WAV file
            if (wavData.Length < 44) // Minimum WAV header size is 44 bytes
            {
                throw new InvalidOperationException("File is too small to be a valid WAV file.");
            }

            // Check WAV file signature: "RIFF" at position 0, "WAVE" at position 8
            string riffHeader = System.Text.Encoding.ASCII.GetString(wavData, 0, 4);
            string waveHeader = System.Text.Encoding.ASCII.GetString(wavData, 8, 4);

            if (riffHeader != "RIFF" || waveHeader != "WAVE")
            {
                throw new InvalidOperationException("File is not a valid WAV file (missing RIFF/WAVE headers).");
            }

            // Find "fmt " chunk (format chunk)
            int fmtChunkIndex = -1;
            for (int i = 12; i < wavData.Length - 8; i++)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavData, i, 4);
                if (chunkId == "fmt ")
                {
                    fmtChunkIndex = i;
                    break;
                }
            }

            if (fmtChunkIndex == -1)
            {
                throw new InvalidOperationException("WAV file is missing format (fmt) chunk.");
            }

            // Read format chunk
            int fmtChunkSize = BitConverter.ToInt32(wavData, fmtChunkIndex + 4);
            int audioFormat = BitConverter.ToInt16(wavData, fmtChunkIndex + 8); // 1 = PCM

            if (audioFormat != 1)
            {
                throw new InvalidOperationException($"Unsupported audio format. Only PCM (format 1) is supported. Found format: {audioFormat}");
            }

            // Read format parameters from fmt chunk
            short channels = BitConverter.ToInt16(wavData, fmtChunkIndex + 10);
            int sampleRate = BitConverter.ToInt32(wavData, fmtChunkIndex + 12);
            short bitsPerSample = BitConverter.ToInt16(wavData, fmtChunkIndex + 22);

            // Find "data" chunk
            int dataChunkIndex = -1;
            for (int i = 12; i < wavData.Length - 4; i++)
            {
                string chunkId = System.Text.Encoding.ASCII.GetString(wavData, i, 4);
                if (chunkId == "data")
                {
                    dataChunkIndex = i;
                    break;
                }
            }

            if (dataChunkIndex == -1)
            {
                throw new InvalidOperationException("WAV file is missing data chunk.");
            }

            // Read data chunk size
            int dataChunkSize = BitConverter.ToInt32(wavData, dataChunkIndex + 4);

            // PCM data starts 8 bytes after "data" (4 bytes for "data" + 4 bytes for chunk size)
            int pcmDataStart = dataChunkIndex + 8;

            // Extract PCM data
            if (pcmDataStart + dataChunkSize > wavData.Length)
            {
                throw new InvalidOperationException("WAV file data chunk size exceeds file length.");
            }

            byte[] pcmData = new byte[dataChunkSize];
            Array.Copy(wavData, pcmDataStart, pcmData, 0, dataChunkSize);

            return new WavFileInfo
            {
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample,
                Channels = channels,
                PcmData = pcmData
            };
        }

        /// <summary>
        /// Gets human-readable description for NoMatch reason
        /// </summary>
        private string GetNoMatchReasonDescription(NoMatchReason reason)
        {
            return reason switch
            {
                NoMatchReason.NotRecognized => "Speech was detected but could not be recognized",
                NoMatchReason.InitialSilenceTimeout => "No speech detected within the initial timeout period",
                NoMatchReason.InitialBabbleTimeout => "Too much background noise detected",
                _ => "Unknown reason"
            };
        }
        private WavFileInfo ConvertWebMToWavWithFFmpeg(byte[] webmData)
        {
            string inputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.webm");
            string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

            try
            {
                File.WriteAllBytes(inputPath, webmData);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-y -i \"{inputPath}\" -ac 1 -ar 16000 -f wav \"{outputPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (!File.Exists(outputPath))
                {
                    throw new Exception("FFmpeg failed to convert WebM to WAV.");
                }

                var wavBytes = File.ReadAllBytes(outputPath);
                return ParseWavFile(wavBytes);
            }
            finally
            {
                try { if (File.Exists(inputPath)) File.Delete(inputPath); } catch { }
                try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
            }
        }
    }
}

