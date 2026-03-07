using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class RecruiterConfiguration : IEntityTypeConfiguration<Recruiter>
    {
        public void Configure(EntityTypeBuilder<Recruiter> builder)
        {
            builder.ToTable("Recruiters");
            builder.HasKey(e => e.AccountId);

            builder.Property(e => e.CompanyName).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.CompanyLogo).HasColumnType("nvarchar(500)").IsRequired(false);
            builder.Property(e => e.Website).HasColumnType("nvarchar(500)").IsRequired(false);
            builder.Property(e => e.Industry).IsRequired().HasColumnType("nvarchar(100)");
            builder.Property(e => e.CompanySize).HasColumnType("nvarchar(100)").IsRequired(false);
            builder.Property(e => e.Address).HasColumnType("nvarchar(500)").IsRequired(false);
            builder.Property(e => e.Phone).HasColumnType("nvarchar(50)").IsRequired(false);
            builder.Property(e => e.VerificationStatus).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
        }
    }
}
