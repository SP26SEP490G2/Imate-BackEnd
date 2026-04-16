using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("Bookings");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.StartTime).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.BookDate).IsRequired();
            builder.Property(e => e.PriceAtBooking).IsRequired();
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.AgoraChannelName).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.AudioRecordKey).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.RatingScore).IsRequired(false);
            builder.Property(e => e.ReviewText).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.RatingCreatedAt).IsRequired(false).HasColumnType("datetimeoffset");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Candidate)
                .WithMany(a => a.CandidateBookings)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Mentor)
                .WithMany(m => m.Bookings)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add Filtered Unique Index to prevent double booking
            // Exclude Cancelled and Refunded bookings from the unique constraint
            builder.HasIndex(e => new { e.MentorId, e.StartTime })
                .HasFilter("[Status] <> 'Cancelled' AND [Status] <> 'Refunded'")
                .IsUnique();
        }
    }
}
