namespace Imate.API.Models.Entities
{
    public class SubscriptionPackage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? DurationDays { get; set; }
        public string? Benefits { get; set; } // JSON stored as nvarchar(max)
        public bool IsActive { get; set; }

        // Navigation properties
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}
