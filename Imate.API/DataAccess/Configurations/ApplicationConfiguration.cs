using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
    {
        public void Configure(EntityTypeBuilder<Application> builder)
        {
            builder.ToTable("Applications");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.ApplicationType).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.Title).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.EvidenceUrls).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.Response).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.User)
                .WithMany(a => a.Applications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Reviewer)
                .WithMany(a => a.ReviewedApplications)
                .HasForeignKey(e => e.ReviewerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Booking)
                .WithMany(b => b.Applications)
                .HasForeignKey(e => e.BookingId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Comment)
                .WithMany(c => c.Applications)
                .HasForeignKey(e => e.CommentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
