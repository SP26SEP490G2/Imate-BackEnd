namespace Imate.API.Infrastructure.Configurations
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        public string Secret { get; init; } = null!;
        public string Issuer { get; init; } = null!;
        public string Audience { get; init; } = null!;
        public int ExpiryMinutes { get; init; }
        public int RefreshTokenExpiryDays { get; init; } = 7; // Mặc định 7 ngày
    }
}
