namespace Imate.API.Models.Entities
{
    public class AccountRole
    {
        public int AccountId { get; set; }
        public int RoleId { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
