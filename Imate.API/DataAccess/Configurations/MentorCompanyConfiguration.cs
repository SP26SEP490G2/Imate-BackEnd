using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class MentorCompanyConfiguration : IEntityTypeConfiguration<MentorCompany>
    {
        public void Configure(EntityTypeBuilder<MentorCompany> builder)
        {
            builder.ToTable("MentorCompanies");
            builder.HasKey(e => new { e.MentorId, e.CompanyId });

            builder.HasOne(e => e.Mentor)
                .WithMany(m => m.MentorCompanies)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Company)
                .WithMany(c => c.MentorCompanies)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
