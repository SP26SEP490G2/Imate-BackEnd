namespace Imate.API.Models.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<MentorCompany> MentorCompanies { get; set; } = new List<MentorCompany>();
        public ICollection<ContributedDetail> ContributedDetails { get; set; } = new List<ContributedDetail>();
    }
}
