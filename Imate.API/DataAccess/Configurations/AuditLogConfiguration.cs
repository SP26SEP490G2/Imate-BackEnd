using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.ActionTime).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.Action).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.EntityType).IsRequired().HasColumnType("nvarchar(100)");
            builder.Property(e => e.EntityId).IsRequired();
            builder.Property(e => e.OldValue).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.NewValue).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.User)
                .WithMany(a => a.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
