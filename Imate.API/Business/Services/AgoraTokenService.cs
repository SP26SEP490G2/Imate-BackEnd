using System;
using Imate.API.Business.Helper;

namespace Imate.API.Business.Services
{
    public class AgoraTokenService
    {
        private readonly string _appId;
        private readonly string _appCertificate;

        public AgoraTokenService(string appId, string appCertificate)
        {
            _appId = appId;
            _appCertificate = appCertificate;

            // ? LOG INITIALIZATION
            Console.WriteLine($"");
            Console.WriteLine($"=== AGORA TOKEN SERVICE INIT ===");
            Console.WriteLine($"App ID: {_appId}");
            Console.WriteLine($"App ID Length: {_appId?.Length ?? 0}");
            Console.WriteLine($"App Certificate: {(string.IsNullOrEmpty(_appCertificate) ? "NOT SET" : "SET")}");
            Console.WriteLine($"================================");
            Console.WriteLine($"");
        }

        /// <summary>
        /// Generate RTC token for Agora Video Call
        /// </summary>
        //public string GenerateRtcToken(string channelName, uint uid, int role, uint privilegeExpiredTs)
        //{
        //    Console.WriteLine($"?? GenerateRtcToken called");
        //    Console.WriteLine($"   Channel: {channelName}");
        //    Console.WriteLine($"   UID: {uid}");
        //    Console.WriteLine($"   Role: {role}");
        //    Console.WriteLine($"   Privilege Expires: {privilegeExpiredTs}");

        //    // If no certificate, return null (works in testing mode)
        //    if (string.IsNullOrEmpty(_appCertificate))
        //    {
        //        Console.WriteLine("??  App Certificate not configured");
        //        Console.WriteLine("   Token generation disabled (testing mode)");
        //        Console.WriteLine("   To enable token authentication:");
        //        Console.WriteLine("   1. Add AppCertificate to appsettings.json");
        //        Console.WriteLine("   2. Enable certificate in Agora Console");
        //        return null;
        //    }

        //    try
        //    {
        //        //var tokenRole = role == 1
        //        //    ? AgoraTokenBuilder.Role.Publisher
        //        //    : AgoraTokenBuilder.Role.Subscriber;

        //        //Console.WriteLine($"?? Building token with certificate...");
        //        //Console.WriteLine($"   Token Role: {tokenRole}");

        //        //var token = AgoraTokenBuilder.BuildTokenWithUid(
        //        //    _appId,
        //        //    _appCertificate,
        //        //    channelName,
        //        //    uid,
        //        //    tokenRole,
        //        //    privilegeExpiredTs
        //        //);

        //        //if (string.IsNullOrEmpty(token))
        //        //{
        //        //    Console.WriteLine($"? Token generation returned null or empty");
        //        //    return null;
        //        //}

        //        //Console.WriteLine($"? Token generated successfully");
        //        //Console.WriteLine($"   Token length: {token.Length}");
        //        //Console.WriteLine($"   Token preview: {token.Substring(0, Math.Min(20, token.Length))}...");

        //        //return token;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"? Error generating token:");
        //        Console.WriteLine($"   Message: {ex.Message}");
        //        Console.WriteLine($"   Type: {ex.GetType().Name}");
        //        Console.WriteLine($"   Stack: {ex.StackTrace}");
        //        return null;
        //    }
        //}

        /// <summary>
        /// Generate token with default 24-hour expiration
        /// </summary>
        //public string GenerateRtcToken(string channelName, uint uid, int role)
        //{
        //    uint privilegeExpiredTs = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 86400); // 24 hours
        //    Console.WriteLine($"?? Generating token with 24-hour expiration");
        //    Console.WriteLine($"   Expires at: {DateTimeOffset.FromUnixTimeSeconds(privilegeExpiredTs):yyyy-MM-dd HH:mm:ss}");

        //    return GenerateRtcToken(channelName, uid, role, privilegeExpiredTs);
        //}
    }

    // Token request model
    public class AgoraTokenRequest
    {
        public string ChannelName { get; set; }
        public uint Uid { get; set; } = 0;
        public int Role { get; set; } = 1;
    }

    // Token response model
    public class AgoraTokenResponse
    {
        public string Token { get; set; }
        public string AppId { get; set; }
        public string ChannelName { get; set; }
        public uint Uid { get; set; }
        public long ExpiresAt { get; set; }
    }
}
