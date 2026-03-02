using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class MentorRecurringSlotConfiguration : IEntityTypeConfiguration<MentorRecurringSlot>
    {
        public void Configure(EntityTypeBuilder<MentorRecurringSlot> builder)
        {
            builder.ToTable("MentorRecurringSlots");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Mentor)
                .WithMany(m => m.MentorRecurringSlots)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Slot)
                .WithMany(s => s.MentorRecurringSlots)
                .HasForeignKey(e => e.SlotId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
