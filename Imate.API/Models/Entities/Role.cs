using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Role
    {
        public int Id { get; set; }
        public RoleName Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
    }
}
