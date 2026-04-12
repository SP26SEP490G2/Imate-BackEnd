using AgoraIO.Media;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Services;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgoraController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AgoraTokenService _agoraTokenService;
        private readonly AgoraRecordingService _agoraRecordingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISystemConfigService _systemConfigService;

        public AgoraController(
            IConfiguration configuration,
            IUnitOfWork unitOfWork,
            ISystemConfigService systemConfigService)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _systemConfigService = systemConfigService;

            var appId = _configuration["Agora:AppId"];
            var appCertificate = _configuration["Agora:AppCertificate"];
            var customerId = _configuration["Agora:CustomerId"] ?? "";
            var customerSecret = _configuration["Agora:CustomerSecret"] ?? "";

            _agoraTokenService = new AgoraTokenService(appId, appCertificate);
            _agoraRecordingService = new AgoraRecordingService(appId, appCertificate, customerId, customerSecret);
        }

        /// <summary>
        /// Get Agora configuration for joining a channel
        /// </summary>
        [HttpPost("token")]
        public async Task<IActionResult> GetToken([FromBody] AgoraTokenRequest request)
        {
            try
            {
                // ? VALIDATION
                if (string.IsNullOrEmpty(request.ChannelName))
                {
                    Console.WriteLine("? ERROR: Channel name is empty");
                    return BadRequest(new { error = "Channel name is required" });
                }

                var appId = _configuration["Agora:AppId"];
                var appCertificate = _configuration["Agora:AppCertificate"];

                // ? DETAILED LOGGING (Gi? nguyên)
                Console.WriteLine($"");
                Console.WriteLine($"=== BACKEND TOKEN GENERATION (Using Official Library) ==="); // C?p nh?t log
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Channel Name: {request.ChannelName}");
                Console.WriteLine($"Requested UID (numeric): {request.Uid}"); // Log UID s? nh?n du?c
                Console.WriteLine($"Role: {request.Role} ({(request.Role == 1 ? "Publisher" : "Subscriber")})");
                Console.WriteLine($"---");
                Console.WriteLine($"App ID: {appId}");
                Console.WriteLine($"App ID Length: {appId?.Length ?? 0}");
                Console.WriteLine($"App ID Type: {appId?.GetType().Name ?? "null"}");
                Console.WriteLine($"App Certificate: {(string.IsNullOrEmpty(appCertificate) ? "NOT SET ??" : "SET ?")}");
                Console.WriteLine($"App Certificate Length: {appCertificate?.Length ?? 0}");
                Console.WriteLine($"==================================================="); // C?p nh?t log

                // ? VALIDATE APP ID & CERTIFICATE (Thêm ki?m tra Certificate)
                if (string.IsNullOrEmpty(appId) || appId.Length != 32)
                {
                    Console.WriteLine($"? ERROR: Invalid App ID format");
                    return StatusCode(500, new { error = "Invalid Agora App ID format" });
                }
                if (string.IsNullOrEmpty(appCertificate)) // R?t quan tr?ng!
                {
                    Console.WriteLine($"? ERROR: App Certificate is missing in configuration.");
                    return StatusCode(500, new { error = "Agora App Certificate not configured" });
                }

                // ? CHU?N B? THAM S? CHO TOKEN BUILDER M?I
                string userAccount = request.Uid.ToString(); // QUAN TR?NG: Chuy?n UID s? sang User Account (string)
                var tokenExpirationHours = await _systemConfigService.GetAgoraTokenExpirationHoursAsync();
                uint tokenExpirationInSeconds = (uint)(tokenExpirationHours * 3600); // Token h?t h?n theo config (gi? -> giây)
                uint privilegeExpirationInSeconds = tokenExpirationInSeconds; // Quy?n cung h?t h?n cùng lúc

                // Xác d?nh Role cho thu vi?n m?i
                RtcTokenBuilder2.Role agoraRole = (request.Role == 1)
                    ? RtcTokenBuilder2.Role.RolePublisher
                    : RtcTokenBuilder2.Role.RoleSubscriber;

                Console.WriteLine($"?? Generating token for User Account: '{userAccount}' with Role: {agoraRole}...");

                // ? G?I HÀM T?O TOKEN T? THU VI?N AGORA CHÍNH TH?C
                string token = RtcTokenBuilder2.buildTokenWithUserAccount(
                    appId,
                    appCertificate,
                    request.ChannelName,
                    userAccount, // Dùng User Account (string)
                    agoraRole,   // Dùng Role enum c?a thu vi?n m?i
                    tokenExpirationInSeconds,
                    privilegeExpirationInSeconds
                );
                // --------------------------------------------------------

                // ? VALIDATE TOKEN (Gi? nguyên)
                if (string.IsNullOrEmpty(token)) // Nên ki?m tra null ho?c r?ng
                {
                    Console.WriteLine($"? ERROR: Generated token is null or empty!");
                    return StatusCode(500, new { error = "Failed to generate token (empty result)" });
                }

                // Tính th?i gian h?t h?n tuy?t d?i d? tr? v? (tùy ch?n)
                var absoluteExpireTime = DateTimeOffset.UtcNow.AddSeconds(tokenExpirationInSeconds);
                long expiresAtTimestamp = absoluteExpireTime.ToUnixTimeSeconds();

                var response = new AgoraTokenResponse
                {
                    Token = token,
                    AppId = appId,
                    ChannelName = request.ChannelName,
                    Uid = request.Uid, // Tr? v? UID s? g?c cho frontend
                    ExpiresAt = expiresAtTimestamp
                };

                // ? LOG SUCCESS (Gi? nguyên)
                Console.WriteLine($"? Token generated successfully");
                Console.WriteLine($"Token: {token.Substring(0, Math.Min(20, token.Length))}...");
                Console.WriteLine($"Token Length: {token.Length}");
                Console.WriteLine($"Expires at (absolute): {absoluteExpireTime:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"===================================================");
                Console.WriteLine($"");

                return Ok(response);
            }
            catch (Exception ex)
            {
                // LOG EXCEPTION (Gi? nguyên)
                Console.WriteLine($"");
                Console.WriteLine($"? EXCEPTION in GetToken");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine($"===================================================");
                Console.WriteLine($"");

                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get Agora App ID (for client initialization)
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var appId = _configuration["Agora:AppId"];

                Console.WriteLine($"");
                Console.WriteLine($"=== GET CONFIG REQUEST ===");
                Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"App ID: {appId}");
                Console.WriteLine($"App ID Length: {appId?.Length ?? 0}");
                Console.WriteLine($"==========================");
                Console.WriteLine($"");

                if (string.IsNullOrEmpty(appId))
                {
                    Console.WriteLine("? ERROR: Agora App ID not configured");
                    return StatusCode(500, new { error = "Agora App ID not configured" });
                }

                return Ok(new { appId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in GetConfig: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get Agora token for a booking (automatic channel name and uid based on booking)
        /// </summary>
        [HttpPost("token/booking/{bookingId}")]
        public async Task<IActionResult> GetTokenForBooking(int bookingId)
        {
            try
            {
                // 1. Get current user ID from JWT token
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { error = "Token không h?p l?" });
                }

                // 2. Get booking with related data
                var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { error = $"Không tìm th?y booking v?i ID {bookingId}" });
                }

                // 3. Validate user has permission (must be candidate or mentor of this booking)
                int userAccountId;
                if (booking.CandidateId == userId)
                {
                    // User is the candidate, use candidate's account ID as UID
                    userAccountId = booking.CandidateId;
                }
                else if (booking.Mentor.AccountId == userId)
                {
                    // User is the mentor, use mentor's account ID as UID
                    userAccountId = booking.Mentor.AccountId;
                }
                else
                {
                    return StatusCode(403, new { error = "B?n không có quy?n truy c?p booking này" });
                }

                // 4. Channel name = booking.Id (as string)
                var channelName = booking.Id.ToString();

                // 5. Get app configuration
                var appId = _configuration["Agora:AppId"];
                var appCertificate = _configuration["Agora:AppCertificate"];

                // 6. Validate configuration
                if (string.IsNullOrEmpty(appId) || appId.Length != 32)
                {
                    return StatusCode(500, new { error = "Invalid Agora App ID format" });
                }
                if (string.IsNullOrEmpty(appCertificate))
                {
                    return StatusCode(500, new { error = "Agora App Certificate not configured" });
                }

                // 7. Calculate token expiration: configured hours after booking start time
                var expirationHours = await _systemConfigService.GetAgoraTokenExpirationHoursAsync();
                var bookingStartTimeOffset = booking.StartTime.ToUniversalTime();
                var tokenExpirationTime = bookingStartTimeOffset.AddHours(expirationHours); // Token expires after configured hours
                var currentTime = DateTimeOffset.UtcNow;

                // Calculate seconds until expiration
                // If start time is in the past, calculate from start time + 1 hour
                // If start time is in the future, calculate from current time to start time + 1 hour
                uint tokenExpirationInSeconds;
                if (tokenExpirationTime <= currentTime)
                {
                    // Token should expire immediately if booking start time + 1 hour has already passed
                    tokenExpirationInSeconds = 0;
                    Console.WriteLine($"??  WARNING: Booking start time + 1 hour has already passed. Token will expire immediately.");
                }
                else
                {
                    // Calculate seconds from now until start time + 1 hour
                    var timeUntilExpiration = tokenExpirationTime - currentTime;
                    tokenExpirationInSeconds = (uint)Math.Min(timeUntilExpiration.TotalSeconds, 86400); // Cap at 24 hours for safety
                }

                uint privilegeExpirationInSeconds = tokenExpirationInSeconds;

                // 8. Convert userAccountId to userAccount (string) for Agora token builder
                string userAccount = userAccountId.ToString();

                // Both candidate and mentor are publishers (role 1)
                RtcTokenBuilder2.Role agoraRole = RtcTokenBuilder2.Role.RolePublisher;

                Console.WriteLine($"?? Generating token for booking {bookingId}");
                Console.WriteLine($"Channel Name: {channelName}");
                Console.WriteLine($"User Account (UID): {userAccount}");
                Console.WriteLine($"Role: Publisher");
                Console.WriteLine($"Booking Start Time: {bookingStartTimeOffset:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"Token Expiration Time: {tokenExpirationTime:yyyy-MM-dd HH:mm:ss} UTC (1 hour after start)");
                Console.WriteLine($"Token Expiration In Seconds: {tokenExpirationInSeconds}");

                string token = RtcTokenBuilder2.buildTokenWithUserAccount(
                    appId,
                    appCertificate,
                    channelName,
                    userAccount,
                    agoraRole,
                    tokenExpirationInSeconds,
                    privilegeExpirationInSeconds
                );

                if (string.IsNullOrEmpty(token))
                {
                    return StatusCode(500, new { error = "Failed to generate token (empty result)" });
                }

                // Set absolute expiration time as start time + 1 hour
                var absoluteExpireTime = tokenExpirationTime;
                long expiresAtTimestamp = absoluteExpireTime.ToUnixTimeSeconds();

                var response = new AgoraTokenResponse
                {
                    Token = token,
                    AppId = appId,
                    ChannelName = channelName,
                    Uid = (uint)userAccountId,
                    ExpiresAt = expiresAtTimestamp
                };

                Console.WriteLine($"? Token generated successfully for booking {bookingId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in GetTokenForBooking");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Validate channel name
        /// </summary>
        [HttpGet("validate-channel/{channelName}")]
        public IActionResult ValidateChannel(string channelName)
        {
            Console.WriteLine($"?? Validating channel: {channelName}");

            // Channel name validation rules
            if (string.IsNullOrEmpty(channelName))
            {
                return BadRequest(new { valid = false, message = "Channel name cannot be empty" });
            }

            if (channelName.Length > 64)
            {
                return BadRequest(new { valid = false, message = "Channel name too long (max 64 chars)" });
            }

            // Check for valid characters (alphanumeric, underscore, hyphen)
            if (!System.Text.RegularExpressions.Regex.IsMatch(channelName, @"^[a-zA-Z0-9_-]+$"))
            {
                return BadRequest(new
                {
                    valid = false,
                    message = "Channel name can only contain letters, numbers, underscore, and hyphen"
                });
            }

            Console.WriteLine($"? Channel name is valid");
            return Ok(new { valid = true, channelName });
        }

        /// <summary>
        /// Start recording for a booking
        /// </summary>
        [HttpPost("recording/start/booking/{bookingId}")]
        public async Task<IActionResult> StartRecordingForBooking(int bookingId)
        {
            try
            {
                // 1. Get current user ID from JWT token
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { error = "Token không h?p l?" });
                }

                // 2. Get booking
                var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { error = $"Không tìm th?y booking v?i ID {bookingId}" });
                }

                // 3. Validate user has permission (must be candidate or mentor)
                if (booking.CandidateId != userId && booking.Mentor.AccountId != userId)
                {
                    return StatusCode(403, new { error = "B?n không có quy?n start recording cho booking này" });
                }

                // 4. Get channel name and recording UID (needed for cleanup check)
                var channelName = booking.Id.ToString();
                var recordingUid = (1000000 + bookingId).ToString();
                
                // 5. Check if there's any old/active recording that needs cleanup
                List<RecordingInfo> sessionList = new List<RecordingInfo>();
                if (!string.IsNullOrEmpty(booking.AudioRecordKey))
                {
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        // Try to parse as a list first
                        try 
                        {
                            sessionList = System.Text.Json.JsonSerializer.Deserialize<List<RecordingInfo>>(booking.AudioRecordKey, options) ?? new List<RecordingInfo>();
                        }
                        catch
                        {
                            // Try to parse as single object (backward compatibility)
                            var single = System.Text.Json.JsonSerializer.Deserialize<RecordingInfo>(booking.AudioRecordKey, options);
                            if (single != null) sessionList.Add(single);
                        }

                        // If any recording is still active, try to stop it
                        var activeRecording = sessionList.FirstOrDefault(r => !r.StoppedAt.HasValue && !string.IsNullOrEmpty(r.Sid));
                        if (activeRecording != null)
                        {
                            Console.WriteLine($"??  Booking {bookingId} has an active recording {activeRecording.Sid}. Attempting to stop it first...");
                            try
                            {
                                await _agoraRecordingService.StopRecordingAsync(activeRecording.ResourceId, activeRecording.Sid, channelName, recordingUid);
                                activeRecording.StoppedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                Console.WriteLine($"? Old recording stopped successfully");
                            }
                            catch (Exception stopEx)
                            {
                                Console.WriteLine($"??  Failed to stop old recording: {stopEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"??  Error handling existing recording data: {ex.Message}");
                    }
                }

                // 6. Generate recording token
                var appId = _configuration["Agora:AppId"];
                var appCertificate = _configuration["Agora:AppCertificate"];

                // Generate token for recording (with publisher role)
                string userAccount = recordingUid;
                uint tokenExpirationInSeconds = 3600; // 1 hour
                uint privilegeExpirationInSeconds = tokenExpirationInSeconds;

                string recordingToken = RtcTokenBuilder2.buildTokenWithUserAccount(
                    appId,
                    appCertificate,
                    channelName,
                    userAccount,
                    RtcTokenBuilder2.Role.RolePublisher,
                    tokenExpirationInSeconds,
                    privilegeExpirationInSeconds
                );

                // 7. Configure storage (using AWS S3)
                // fileNamePrefix must contain only alphanumeric characters (no special chars like _, -, spaces)
                // Add "recordings" folder prefix to organize files
                var fileNamePrefix1 = "recordings"; // Folder name for recordings
                var fileNamePrefix2 = $"booking{bookingId}"; // Remove underscore
                var fileNamePrefix3 = channelName; // Already numeric
                // Use AWS S3 for Agora Cloud Recording
                var storageConfig = new RecordingStorageConfig
                {
                    Vendor = 1, // AWS S3
                    Region = 9, // ap-southeast-2 (Sydney) matches user's config
                    Bucket = _configuration["AwsS3Storage:BucketName"] ?? "",
                    AccessKey = _configuration["AwsS3Storage:AccessKey"] ?? "",
                    SecretKey = _configuration["AwsS3Storage:SecretKey"] ?? "",
                    FileNamePrefix = new string[] { fileNamePrefix1, fileNamePrefix2, fileNamePrefix3 }
                };

                // 7. Start recording
                var recordingResponse = await _agoraRecordingService.StartRecordingAsync(
                    channelName,
                    recordingUid,
                    recordingToken,
                    storageConfig
                );

                // 8. Save recording info to booking (Append to list)
                var newSession = new RecordingInfo
                {
                    ResourceId = recordingResponse.ResourceId,
                    Sid = recordingResponse.Sid,
                    ChannelName = channelName,
                    Uid = recordingUid,
                    StartedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                sessionList.Add(newSession);
                
                booking.AudioRecordKey = System.Text.Json.JsonSerializer.Serialize(sessionList);
                await _unitOfWork.SaveChangesAsync();

                Console.WriteLine($"? Recording started successfully for booking {bookingId}");
                Console.WriteLine($"Resource ID: {recordingResponse.ResourceId}");
                Console.WriteLine($"SID: {recordingResponse.Sid}");

                return Ok(new
                {
                    success = true,
                    bookingId = bookingId,
                    resourceId = recordingResponse.ResourceId,
                    sid = recordingResponse.Sid,
                    channelName = channelName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in StartRecordingForBooking");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Stop recording for a booking
        /// </summary>
        [HttpPost("recording/stop/booking/{bookingId}")]
        public async Task<IActionResult> StopRecordingForBooking(int bookingId)
        {
            try
            {
                // 1. Get current user ID from JWT token
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new { error = "Token không h?p l?" });
                }

                // 2. Get booking
                var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { error = $"Không tìm th?y booking v?i ID {bookingId}" });
                }

                // 3. Validate user has permission
                if (booking.CandidateId != userId && booking.Mentor.AccountId != userId)
                {
                    return StatusCode(403, new { error = "B?n không có quy?n stop recording cho booking này" });
                }

                // 4. Check if recording exists
                if (string.IsNullOrEmpty(booking.AudioRecordKey))
                {
                    return BadRequest(new { error = "Không tìm th?y recording cho booking này" });
                }

                // 5. Parse recording info from AudioRecordKey (stored as JSON)
                try
                {
                    Console.WriteLine($"?? Parsing recording info from AudioRecordKey: {booking.AudioRecordKey?.Substring(0, Math.Min(100, booking.AudioRecordKey?.Length ?? 0))}...");
                    
                    List<RecordingInfo> sessionList = new List<RecordingInfo>();
                    RecordingInfo? activeRecording = null;
                    
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        try 
                        {
                            sessionList = System.Text.Json.JsonSerializer.Deserialize<List<RecordingInfo>>(booking.AudioRecordKey, options) ?? new List<RecordingInfo>();
                        }
                        catch
                        {
                            var single = System.Text.Json.JsonSerializer.Deserialize<RecordingInfo>(booking.AudioRecordKey, options);
                            if (single != null) sessionList.Add(single);
                        }

                        // For StopRecordingForBooking, we usually want to stop the LATEST active session
                        activeRecording = sessionList.LastOrDefault(r => !r.StoppedAt.HasValue);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        Console.WriteLine($"??  Failed to parse recording info: {ex.Message}");
                        return BadRequest(new { error = "Recording info không h?p l? ho?c d?nh d?ng cu." });
                    }

                    if (activeRecording == null)
                    {
                        Console.WriteLine($"? No active recording session found to stop");
                        return Ok(new { success = true, message = "Không tìm th?y session dang record d? stop" });
                    }
                    
                    if (string.IsNullOrEmpty(activeRecording.ResourceId))
                    {
                        return BadRequest(new { error = "Recording info thi?u ResourceId" });
                    }
                    
                    if (string.IsNullOrEmpty(activeRecording.Sid))
                    {
                        return BadRequest(new { error = "Recording info thi?u SID" });
                    }
                    
                    Console.WriteLine($"? Parsed recording info - ResourceId: {activeRecording.ResourceId}, SID: {activeRecording.Sid}");

                    var channelName = booking.Id.ToString();
                    var uid = activeRecording.Uid;

                    // Stop recording
                    AgoraRecordingStopResponse? response = null;
                    
                    try
                    {
                        response = await _agoraRecordingService.StopRecordingAsync(
                            activeRecording.ResourceId,
                            activeRecording.Sid,
                            channelName,
                            uid
                        );
                    }
                    catch (Exception stopEx)
                    {
                        Console.WriteLine($"??  Failed to stop recording via API: {stopEx.Message}");
                        
                        // Check if it's a "failed to find worker" error (recording already stopped or expired)
                        if (stopEx.Message.Contains("failed to find worker") || stopEx.Message.Contains("404"))
                        {
                            Console.WriteLine($"??  Recording session not found. It may have already stopped or expired.");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    // Helper function to create file URL from AWS S3
                    string CreateFileUrl(string fileName)
                    {
                        var bucketName = _configuration["AwsS3Storage:BucketName"] ?? "";
                        var regionName = _configuration["AwsS3Storage:RegionName"] ?? "ap-southeast-1";
                        // AWS S3 URL format: https://{bucket}.s3.{region}.amazonaws.com/{filePath}
                        // FileName already contains the folder prefix from fileNamePrefix
                        return $"https://{bucketName}.s3.{regionName}.amazonaws.com/{fileName}";
                    }

                    // Update recording session with result
                    activeRecording.StoppedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    
                    // NEW: Save file list to database so we don't have null in the record key
                    if (response?.Files != null && response.Files.Any())
                    {
                        activeRecording.Files = response.Files.Select(f => new RecordingFileInfo
                        {
                            FileName = f.FileName,
                            TrackType = f.TrackType,
                            IsPlayable = f.IsPlayable
                        }).ToList();
                    }

                    booking.AudioRecordKey = System.Text.Json.JsonSerializer.Serialize(sessionList);
                    
                    await _unitOfWork.SaveChangesAsync();

                    Console.WriteLine($"? Recording stopped successfully for booking {bookingId}");
                    
                    // Map files to include URLs
                    var filesWithUrls = response?.Files?.Select(f => new
                    {
                        fileName = f.FileName,
                        trackType = f.TrackType,
                        isPlayable = f.IsPlayable,
                        url = CreateFileUrl(f.FileName)
                    }).ToArray();

                    return Ok(new
                    {
                        success = true,
                        bookingId = bookingId,
                        resourceId = activeRecording.ResourceId,
                        sid = activeRecording.Sid,
                        files = filesWithUrls
                    });
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"? Error parsing recording info: {ex.Message}");
                    return BadRequest(new { error = "L?i khi parse recording info. Có th? format cu không tuong thích." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in StopRecordingForBooking");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Stop recording with ResourceId and SID
        /// </summary>
        [HttpPost("recording/stop")]
        public async Task<IActionResult> StopRecording([FromBody] StopRecordingRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ResourceId) || string.IsNullOrEmpty(request.Sid))
                {
                    return BadRequest(new { error = "ResourceId and Sid are required" });
                }

                // Stop recording
                var response = await _agoraRecordingService.StopRecordingAsync(
                    request.ResourceId,
                    request.Sid,
                    request.ChannelName,
                    request.Uid
                );

                // Update booking if provided
                if (request.BookingId.HasValue)
                {
                    var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(request.BookingId.Value);
                    if (booking != null && booking.AudioRecordKey == request.Sid)
                    {
                        // Update with file information if available
                        if (response.Files != null && response.Files.Any())
                        {
                            // Store file information (you might want to create a RecordingFiles table)
                            var fileInfo = string.Join(";", response.Files.Select(f => f.FileName));
                            booking.AudioRecordKey = $"{request.Sid}|{fileInfo}"; // Store SID and file info
                        }
                        
                        // Update booking status to Completed if AudioRecordKey is not null
                        if (!string.IsNullOrEmpty(booking.AudioRecordKey))
                        {
                            booking.Status = BookingStatus.Completed;
                        }
                        
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                Console.WriteLine($"? Recording stopped successfully");
                Console.WriteLine($"Resource ID: {response.ResourceId}");
                Console.WriteLine($"SID: {response.Sid}");

                return Ok(new
                {
                    success = true,
                    resourceId = response.ResourceId,
                    sid = response.Sid,
                    files = response.Files
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in StopRecording");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get recording info for a booking
        /// </summary>
        [HttpGet("recording/booking/{bookingId}")]
        public async Task<IActionResult> GetRecordingInfo(int bookingId)
        {
            try
            {
                var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return NotFound(new { error = "Booking không t?n t?i" });
                }

                if (string.IsNullOrEmpty(booking.AudioRecordKey))
                {
                    return Ok(new { hasRecording = false, message = "Không có recording cho booking này" });
                }

                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    List<RecordingInfo> sessionList = new List<RecordingInfo>();
                    try 
                    {
                        sessionList = System.Text.Json.JsonSerializer.Deserialize<List<RecordingInfo>>(booking.AudioRecordKey, options) ?? new List<RecordingInfo>();
                    }
                    catch
                    {
                        var single = System.Text.Json.JsonSerializer.Deserialize<RecordingInfo>(booking.AudioRecordKey, options);
                        if (single != null) sessionList.Add(single);
                    }

                    // Helper function to create file URL from AWS S3
                    string CreateFileUrl(string fileName)
                    {
                        var bucketName = _configuration["AwsS3Storage:BucketName"] ?? "";
                        var regionName = _configuration["AwsS3Storage:RegionName"] ?? "ap-southeast-1";
                        return $"https://{bucketName}.s3.{regionName}.amazonaws.com/{fileName}";
                    }

                    var allRecordings = sessionList.Select(session => new
                    {
                        resourceId = session.ResourceId,
                        sid = session.Sid,
                        channelName = session.ChannelName,
                        uid = session.Uid,
                        startedAt = session.StartedAt,
                        stoppedAt = session.StoppedAt,
                        files = session.Files?.Select(f => new
                        {
                            fileName = f.FileName,
                            trackType = f.TrackType,
                            isPlayable = f.IsPlayable,
                            url = CreateFileUrl(f.FileName)
                        }).ToArray()
                    }).ToList();

                    return Ok(new
                    {
                        hasRecording = true,
                        count = allRecordings.Count,
                        recordings = allRecordings,
                        // For backward compatibility, also return the latest one at root
                        latest = allRecordings.LastOrDefault()
                    });
                }
                catch (System.Text.Json.JsonException)
                {
                    return Ok(new { hasRecording = false, message = "Recording info d?nh d?ng không h?p l?" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in GetRecordingInfo");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Query recording status
        /// </summary>
        [HttpGet("recording/status")]
        public async Task<IActionResult> QueryRecordingStatus([FromQuery] string resourceId, [FromQuery] string sid)
        {
            try
            {
                if (string.IsNullOrEmpty(resourceId) || string.IsNullOrEmpty(sid))
                {
                    return BadRequest(new { error = "ResourceId and Sid are required" });
                }

                var response = await _agoraRecordingService.QueryRecordingAsync(resourceId, sid);

                return Ok(new
                {
                    resourceId = response.ResourceId,
                    sid = response.Sid,
                    status = response.Status,
                    files = response.Files
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? EXCEPTION in QueryRecordingStatus");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }
    }

    // Request models for recording
    public class StopRecordingRequest
    {
        public string ResourceId { get; set; } = "";
        public string Sid { get; set; } = "";
        public string ChannelName { get; set; } = "";
        public string Uid { get; set; } = "";
        public int? BookingId { get; set; }
    }

    // Recording info model (stored in AudioRecordKey)
    public class RecordingInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("resourceId")]
        public string ResourceId { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("sid")]
        public string Sid { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("channelName")]
        public string ChannelName { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("uid")]
        public string Uid { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("startedAt")]
        public long StartedAt { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("stoppedAt")]
        public long? StoppedAt { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("files")]
        public List<RecordingFileInfo>? Files { get; set; }
    }

    public class RecordingFileInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("trackType")]
        public string TrackType { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("isPlayable")]
        public bool IsPlayable { get; set; }
    }
}
