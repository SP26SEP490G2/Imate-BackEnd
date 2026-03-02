namespace Imate.API.Models.Entities
{
    public class MentorCompany
    {
        public int MentorId { get; set; }
        public int CompanyId { get; set; }

        // Navigation properties
        public Mentor Mentor { get; set; } = null!;
        public Company Company { get; set; } = null!;
    }
}
