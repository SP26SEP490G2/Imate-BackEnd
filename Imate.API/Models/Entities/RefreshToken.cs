namespace Imate.API.Models.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTimeOffset? RevokedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
    }
}
