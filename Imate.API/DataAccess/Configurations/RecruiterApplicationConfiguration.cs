using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class RecruiterApplicationConfiguration : IEntityTypeConfiguration<RecruiterApplication>
    {
        public void Configure(EntityTypeBuilder<RecruiterApplication> builder)
        {
            builder.ToTable("RecruiterApplications");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.CompanyName).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.BusinessLicenseUrl).IsRequired().HasColumnType("nvarchar(500)");
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.Notes).HasColumnType("nvarchar(max)").IsRequired(false);

            builder.HasOne(e => e.User)
                .WithMany(a => a.RecruiterApplications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Reviewer)
                .WithMany(a => a.ReviewedRecruiterApplications)
                .HasForeignKey(e => e.ReviewerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
