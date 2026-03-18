using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;

namespace Imate.API.Business.Services
{
    /// <summary>
    /// Service Ä‘á»ƒ quáº£n lÃ½ Agora Cloud Recording
    /// Sá»­ dá»¥ng Agora Cloud Recording REST API Ä‘á»ƒ start/stop recording
    /// </summary>
    public class AgoraRecordingService
    {
        private readonly string _appId;
        private readonly string _appCertificate;
        private readonly string _customerId;
        private readonly string _customerSecret;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.agora.io/v1";

        public AgoraRecordingService(
            string appId,
            string appCertificate,
            string customerId,
            string customerSecret)
        {
            _appId = appId;
            _appCertificate = appCertificate;
            _customerId = customerId;
            _customerSecret = customerSecret;
            _httpClient = new HttpClient();
            
            // Set up Basic Authentication
            var authBytes = Encoding.UTF8.GetBytes($"{_customerId}:{_customerSecret}");
            var authHeader = Convert.ToBase64String(authBytes);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            
            Console.WriteLine($"ðŸ”µ AgoraRecordingService initialized");
            Console.WriteLine($"App ID: {_appId}");
            Console.WriteLine($"Customer ID: {(_customerId.Length > 0 ? _customerId.Substring(0, Math.Min(10, _customerId.Length)) + "..." : "EMPTY")}");
            Console.WriteLine($"Customer Secret: {(_customerSecret.Length > 0 ? "***" : "EMPTY")}");
        }


        /// <summary>
        /// Start recording vá»›i storage configuration
        /// </summary>
        public async Task<AgoraRecordingStartResponse> StartRecordingAsync(
            string channelName,
            string uid,
            string token,
            RecordingStorageConfig storageConfig,
            RecordingMode mode = RecordingMode.Mix)
        {
            try
            {
                Console.WriteLine($"ðŸ”µ Starting recording for channel: {channelName}, UID: {uid}");

                var requestBody = new
                {
                    cname = channelName,
                    uid = uid,
                    clientRequest = new
                    {
                        token = token,
                        recordingConfig = new
                        {
                            maxIdleTime = 30,
                            streamTypes = 2, // 2 = audio and video
                            audioProfile = 1, // 1 = high quality audio
                            channelType = 0, // 0 = communication
                            videoStreamType = 0, // 0 = low stream
                            subscribeVideoUids = new string[] { },
                            subscribeAudioUids = new string[] { }
                        },
                        storageConfig = new
                        {
                            vendor = storageConfig.Vendor,
                            region = storageConfig.Region,
                            bucket = storageConfig.Bucket,
                            accessKey = storageConfig.AccessKey,
                            secretKey = storageConfig.SecretKey,
                            fileNamePrefix = storageConfig.FileNamePrefix ?? new string[] { channelName }
                        }
                    }
                };

                var acquireRequestBody = new
                {
                    cname = channelName,
                    uid = uid,
                    clientRequest = new { }
                };

                var acquireJson = JsonSerializer.Serialize(acquireRequestBody);
                Console.WriteLine($"ðŸ”µ Acquire request JSON: {acquireJson}");
                var acquireContent = new StringContent(acquireJson, Encoding.UTF8, "application/json");

                // Step 1: Acquire resource
                var acquireResponse = await _httpClient.PostAsync(
                    $"{_baseUrl}/apps/{_appId}/cloud_recording/acquire",
                    acquireContent);

                var acquireResponseContent = await acquireResponse.Content.ReadAsStringAsync();
                
                Console.WriteLine($"ðŸ”µ Acquire response status: {acquireResponse.StatusCode}");
                Console.WriteLine($"ðŸ”µ Acquire response content: {acquireResponseContent}");

                if (!acquireResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"âŒ Failed to acquire recording resource: {acquireResponse.StatusCode}");
                    Console.WriteLine($"Response: {acquireResponseContent}");
                    throw new Exception($"Failed to acquire recording resource: {acquireResponseContent}");
                }

                var acquireResult = JsonSerializer.Deserialize<AgoraAcquireResponse>(acquireResponseContent);
                var resourceId = acquireResult?.ResourceId ?? acquireResult?.Resource_id;

                if (string.IsNullOrEmpty(resourceId))
                {
                    Console.WriteLine($"âŒ Failed to parse resource ID. Response was: {acquireResponseContent}");
                    throw new Exception($"Failed to acquire resource ID. Response: {acquireResponseContent}");
                }

                Console.WriteLine($"âœ… Resource acquired. Resource ID: {resourceId}");

                // Step 2: Start recording
                var startRequestBody = new
                {
                    cname = channelName,
                    uid = uid,
                    clientRequest = new
                    {
                        token = token,
                        recordingConfig = new
                        {
                            maxIdleTime = 300, // 5 minutes instead of 30 seconds
                            streamTypes = 2,
                            audioProfile = 1,
                            channelType = 0,
                            videoStreamType = 0,
                            subscribeVideoUids = new string[] { },
                            subscribeAudioUids = new string[] { },
                            transcodingConfig = new
                            {
                                width = 640,
                                height = 480,
                                fps = 30,
                                bitrate = 600,
                                mixedVideoLayout = 1,
                                backgroundColor = "#000000"
                            }
                        },
                        storageConfig = new
                        {
                            vendor = storageConfig.Vendor,
                            region = storageConfig.Region,
                            bucket = storageConfig.Bucket,
                            accessKey = storageConfig.AccessKey,
                            secretKey = storageConfig.SecretKey,
                            fileNamePrefix = storageConfig.FileNamePrefix ?? new string[] { channelName }
                        },
                        recordingFileConfig = new
                        {
                            avFileType = new string[] { "hls", "mp4" }
                        }
                    }
                };

                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var startJson = JsonSerializer.Serialize(startRequestBody, options);
                Console.WriteLine($"ðŸ”µ Start recording request JSON: {startJson}");
                var startContent = new StringContent(startJson, Encoding.UTF8, "application/json");

                var startResponse = await _httpClient.PostAsync(
                    $"{_baseUrl}/apps/{_appId}/cloud_recording/resourceid/{resourceId}/mode/mix/start",
                    startContent);

                var startResponseContent = await startResponse.Content.ReadAsStringAsync();
                
                Console.WriteLine($"ðŸ”µ Start recording response status: {startResponse.StatusCode}");
                Console.WriteLine($"ðŸ”µ Start recording response content: {startResponseContent}");

                if (!startResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"âŒ Failed to start recording: {startResponse.StatusCode}");
                    Console.WriteLine($"Response: {startResponseContent}");
                    throw new Exception($"Failed to start recording: {startResponseContent}");
                }

                var result = JsonSerializer.Deserialize<AgoraRecordingStartResponse>(startResponseContent);
                
                // Parse SID from response - Agora API might return it in different field
                var sid = result?.Sid ?? "";
                
                // If SID is empty, try to parse from serverResponse or check response directly
                if (string.IsNullOrEmpty(sid))
                {
                    // Try parsing as dynamic object to see all fields
                    var jsonDoc = JsonDocument.Parse(startResponseContent);
                    if (jsonDoc.RootElement.TryGetProperty("sid", out var sidElement))
                    {
                        sid = sidElement.GetString() ?? "";
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("serverResponse", out var serverResponseElement))
                    {
                        // SID might be in serverResponse object
                        if (serverResponseElement.ValueKind == JsonValueKind.Object && 
                            serverResponseElement.TryGetProperty("sid", out var sidInResponse))
                        {
                            sid = sidInResponse.GetString() ?? "";
                        }
                    }
                    
                    Console.WriteLine($"ðŸ”µ Parsed SID: {sid}");
                }
                
                Console.WriteLine($"âœ… Recording started successfully. Resource ID: {resourceId}, SID: {sid}");

                return new AgoraRecordingStartResponse
                {
                    ResourceId = resourceId,
                    Sid = sid
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âŒ Error starting recording: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public async Task<AgoraRecordingStopResponse> StopRecordingAsync(
            string resourceId,
            string sid,
            string channelName,
            string uid)
        {
            try
            {
                Console.WriteLine($"ðŸ”µ Stopping recording. Resource ID: {resourceId}, SID: {sid}");

                var requestBody = new
                {
                    cname = channelName,
                    uid = uid,
                    clientRequest = new { }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/apps/{_appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/mix/stop",
                    content);

                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"ðŸ”µ Stop recording response status: {response.StatusCode}");
                Console.WriteLine($"ðŸ”µ Stop recording response content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"âŒ Failed to stop recording: {response.StatusCode}");
                    Console.WriteLine($"Response: {responseContent}");
                    throw new Exception($"Failed to stop recording: {responseContent}");
                }

                var result = JsonSerializer.Deserialize<AgoraRecordingStopResponse>(responseContent);
                
                // Check for error codes in response
                if (result?.Code == 65)
                {
                    Console.WriteLine($"âš ï¸  code:65 - recording still uploading. Waiting 60 seconds and querying...");
                    // Wait for recording to complete upload
                    await Task.Delay(60000);
                    try
                    {
                        var queryResult = await QueryRecordingAsync(resourceId, sid);
                        // Use query result instead of stop result if it has files
                        if (queryResult?.Files != null && queryResult.Files.Any())
                        {
                            Console.WriteLine($"âœ… Found {queryResult.Files.Count} files after query");
                            result.Files = queryResult.Files;
                        }
                        else
                        {
                            Console.WriteLine($"âš ï¸  No files found after query. Recording may have failed or no stream was captured.");
                        }
                    }
                    catch (Exception queryEx)
                    {
                        Console.WriteLine($"âš ï¸  Query failed: {queryEx.Message}. Files may be uploaded later.");
                    }
                }
                
                Console.WriteLine($"âœ… Recording stopped successfully");
                
                // Log serverResponse to check for file information
                if (result?.ServerResponse != null)
                {
                    Console.WriteLine($"ðŸ”µ Server response - UploadingStatus: {result.ServerResponse.UploadingStatus}");
                    Console.WriteLine($"ðŸ”µ Server response - FileListMode: {result.ServerResponse.FileListMode}");
                    Console.WriteLine($"ðŸ”µ Server response - FileList: {result.ServerResponse.FileList}");
                    
                    // Try to parse fileList based on mode
                    if (result.ServerResponse.FileList != null)
                    {
                        try
                        {
                            // If FileListMode is "json", FileList is already an array
                            if (result.ServerResponse.FileListMode == "json")
                            {
                                var fileListJson = result.ServerResponse.FileList.ToString() ?? "[]";
                                var filesArray = JsonSerializer.Deserialize<List<RecordingFile>>(fileListJson);
                                if (filesArray != null && filesArray.Any())
                                {
                                    Console.WriteLine($"ðŸ“ Parsed {filesArray.Count} files from fileList (JSON mode):");
                                    result.Files = filesArray;
                                    foreach (var file in filesArray)
                                    {
                                        Console.WriteLine($"   - {file.FileName} ({file.TrackType}) - Playable: {file.IsPlayable}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"âš ï¸  fileList is empty array");
                                }
                            }
                            else if (result.ServerResponse.FileListMode == "string" && result.ServerResponse.FileList is string fileListStr)
                            {
                                // If FileListMode is "string", FileList is a JSON string
                                if (!string.IsNullOrEmpty(fileListStr))
                                {
                                    var filesArray = JsonSerializer.Deserialize<List<RecordingFile>>(fileListStr);
                                    if (filesArray != null && filesArray.Any())
                                    {
                                        Console.WriteLine($"ðŸ“ Parsed {filesArray.Count} files from fileList (string mode):");
                                        result.Files = filesArray;
                                        foreach (var file in filesArray)
                                        {
                                            Console.WriteLine($"   - {file.FileName} ({file.TrackType}) - Playable: {file.IsPlayable}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Console.WriteLine($"âš ï¸  Could not parse fileList: {parseEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"âš ï¸  fileList is null");
                    }
                }
                
                if (result?.Files != null && result.Files.Any())
                {
                    Console.WriteLine($"ðŸ“ Recording files: {result.Files.Count} files");
                    foreach (var file in result.Files)
                    {
                        Console.WriteLine($"   - {file.FileName} ({file.TrackType})");
                    }
                }
                else
                {
                    Console.WriteLine($"âš ï¸  No files in stop response. Files may be uploaded later.");
                    Console.WriteLine($"   Full response content: {responseContent}");
                }

                return result ?? throw new Exception("Invalid response from Agora API");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âŒ Error stopping recording: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Query recording status
        /// </summary>
        public async Task<AgoraRecordingQueryResponse> QueryRecordingAsync(
            string resourceId,
            string sid)
        {
            try
            {
                Console.WriteLine($"ðŸ”µ Querying recording status. Resource ID: {resourceId}, SID: {sid}");

                var response = await _httpClient.GetAsync(
                    $"{_baseUrl}/apps/{_appId}/cloud_recording/resourceid/{resourceId}/sid/{sid}/mode/mix/query");

                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"ðŸ”µ Query recording response status: {response.StatusCode}");
                Console.WriteLine($"ðŸ”µ Query recording response content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"âŒ Failed to query recording: {response.StatusCode}");
                    Console.WriteLine($"Response: {responseContent}");
                    throw new Exception($"Failed to query recording: {responseContent}");
                }

                var result = JsonSerializer.Deserialize<AgoraRecordingQueryResponse>(responseContent);
                Console.WriteLine($"âœ… Recording status queried successfully");
                
                // Parse fileList from serverResponse if available
                if (result?.ServerResponse != null)
                {
                    Console.WriteLine($"ðŸ”µ Server response - UploadingStatus: {result.ServerResponse.UploadingStatus}");
                    Console.WriteLine($"ðŸ”µ Server response - FileListMode: {result.ServerResponse.FileListMode}");
                    Console.WriteLine($"ðŸ”µ Server response - FileList: {result.ServerResponse.FileList}");
                    
                    if (result.ServerResponse.FileList != null)
                    {
                        try
                        {
                            if (result.ServerResponse.FileListMode == "json")
                            {
                                var fileListJson = result.ServerResponse.FileList.ToString() ?? "[]";
                                var filesArray = JsonSerializer.Deserialize<List<RecordingFile>>(fileListJson);
                                if (filesArray != null && filesArray.Any())
                                {
                                    Console.WriteLine($"ðŸ“ Parsed {filesArray.Count} files from fileList (JSON mode):");
                                    result.Files = filesArray;
                                    foreach (var file in filesArray)
                                    {
                                        Console.WriteLine($"   - {file.FileName} ({file.TrackType}) - Playable: {file.IsPlayable}");
                                    }
                                }
                            }
                            else if (result.ServerResponse.FileListMode == "string" && result.ServerResponse.FileList is string fileListStr)
                            {
                                if (!string.IsNullOrEmpty(fileListStr))
                                {
                                    var filesArray = JsonSerializer.Deserialize<List<RecordingFile>>(fileListStr);
                                    if (filesArray != null && filesArray.Any())
                                    {
                                        Console.WriteLine($"ðŸ“ Parsed {filesArray.Count} files from fileList (string mode):");
                                        result.Files = filesArray;
                                        foreach (var file in filesArray)
                                        {
                                            Console.WriteLine($"   - {file.FileName} ({file.TrackType}) - Playable: {file.IsPlayable}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            Console.WriteLine($"âš ï¸  Could not parse fileList: {parseEx.Message}");
                        }
                    }
                }
                
                if (result?.Files != null && result.Files.Any())
                {
                    Console.WriteLine($"ðŸ“ Recording files: {result.Files.Count} files");
                    foreach (var file in result.Files)
                    {
                        Console.WriteLine($"   - {file.FileName} ({file.TrackType}) - Playable: {file.IsPlayable}");
                    }
                }

                return result ?? throw new Exception("Invalid response from Agora API");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âŒ Error querying recording: {ex.Message}");
                throw;
            }
        }
    }

    // Enums
    public enum RecordingMode
    {
        Individual = 0,
        Mix = 1,
        Web = 2
    }

    // Models
    public class RecordingStorageConfig
    {
        public int Vendor { get; set; } // 1 = AWS S3, 2 = Alibaba Cloud, 3 = Tencent Cloud, etc.
        public int Region { get; set; }
        public string Bucket { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string[]? FileNamePrefix { get; set; }
    }

    public class AgoraAcquireResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("resourceId")]
        public string ResourceId { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("resource_id")]
        public string Resource_id { get; set; } = "";
    }

    public class AgoraRecordingStartResponse
    {
        public string ResourceId { get; set; } = "";
        public string Sid { get; set; } = "";
        public string ServerResponse { get; set; } = "";
    }

    public class AgoraRecordingStopResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public int? Code { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("resourceId")]
        public string ResourceId { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("sid")]
        public string Sid { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("serverResponse")]
        public ServerResponseContent? ServerResponse { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("fileList")]
        public List<RecordingFile>? Files { get; set; }
    }
    
    public class ServerResponseContent
    {
        [System.Text.Json.Serialization.JsonPropertyName("fileList")]
        public object? FileList { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("fileListMode")]
        public string? FileListMode { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("uploadingStatus")]
        public string? UploadingStatus { get; set; }
    }

    public class AgoraRecordingQueryResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("resourceId")]
        public string ResourceId { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("sid")]
        public string Sid { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public int Status { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("serverResponse")]
        public ServerResponseContent? ServerResponse { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("fileList")]
        public List<RecordingFile>? Files { get; set; }
    }

    public class RecordingFile
    {
        [System.Text.Json.Serialization.JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("trackType")]
        public string TrackType { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        public string Uid { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("mixedAllUser")]
        public bool MixedAllUser { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("isPlayable")]
        public bool IsPlayable { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("sliceStartTime")]
        public long SliceStartTime { get; set; }
    }
}

