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

            // Seed dữ liệu mặc định cho SystemConfig
            var now = new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero);
            builder.HasData(
                new SystemConfig { Id = 1, Key = "COMMISSION_RATE", Value = "20.0", Description = "Tỷ lệ hoa hồng (%)", CreatedAt = now },
                new SystemConfig { Id = 2, Key = "FREE_INTERVIEW_LIMIT", Value = "3", Description = "Số lượt phỏng vấn miễn phí mặc định", CreatedAt = now },
                new SystemConfig { Id = 4, Key = "ESCROW_HOURS", Value = "24", Description = "Thời gian khóa tiền sau khi hoàn thành (giờ)", CreatedAt = now },
                new SystemConfig { Id = 5, Key = "MIN_BOOKING_ADVANCE_HOURS", Value = "6", Description = "Thời gian đặt lịch trước tối thiểu (giờ)", CreatedAt = now },
                new SystemConfig { Id = 9, Key = "MIN_DEPOSIT_AMOUNT", Value = "1000", Description = "Số tiền nạp tối thiểu (VNĐ)", CreatedAt = now }            );
        }
    }
}
