using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class SystemNotificationConfiguration : IEntityTypeConfiguration<SystemNotification>
    {
        public void Configure(EntityTypeBuilder<SystemNotification> builder)
        {
            builder.ToTable("SystemNotifications");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.Type).IsRequired().HasColumnType("nvarchar(100)");
            builder.Property(e => e.Message).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.Link).HasColumnType("nvarchar(500)").IsRequired(false);
            builder.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");

            builder.HasOne(e => e.RecipientUser)
                .WithMany(a => a.ReceivedNotifications)
                .HasForeignKey(e => e.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.TriggerByUser)
                .WithMany(a => a.TriggeredNotifications)
                .HasForeignKey(e => e.TriggerByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
