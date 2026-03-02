using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
    {
        public void Configure(EntityTypeBuilder<SystemConfig> builder)
        {
            builder.ToTable("SystemConfigs");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .UseIdentityColumn();

            builder.Property(e => e.Key)
                .IsRequired()
                .HasColumnType("nvarchar(255)");

            builder.HasIndex(e => e.Key)
                .IsUnique();

            builder.Property(e => e.Value)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.Description)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetimeoffset");

            builder.Property(e => e.UpdatedAt)
                .IsRequired(false)
                .HasColumnType("datetimeoffset");
        }
    }
}
