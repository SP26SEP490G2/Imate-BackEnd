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
                new SystemConfig { Id = 3, Key = "CONTRIBUTION_REWARD_POINTS", Value = "1000", Description = "Điểm thưởng khi đóng góp câu hỏi", CreatedAt = now },
                new SystemConfig { Id = 4, Key = "ESCROW_HOURS", Value = "24", Description = "Thời gian khóa tiền sau khi hoàn thành (giờ)", CreatedAt = now },
                new SystemConfig { Id = 5, Key = "MIN_BOOKING_ADVANCE_HOURS", Value = "6", Description = "Thời gian đặt lịch trước tối thiểu (giờ)", CreatedAt = now },
                new SystemConfig { Id = 6, Key = "CANCELLATION_DEADLINE_HOURS", Value = "24", Description = "Thời hạn hủy phạt (giờ trước khi bắt đầu)", CreatedAt = now },
                new SystemConfig { Id = 7, Key = "REPORT_DEADLINE_HOURS", Value = "24", Description = "Thời hạn khiếu nại sau khi hoàn thành (giờ)", CreatedAt = now },
                new SystemConfig { Id = 8, Key = "CANCELLATION_REFUND_RATE", Value = "80", Description = "Tỷ lệ hoàn tiền khi hủy chậm (%)", CreatedAt = now },
                new SystemConfig { Id = 9, Key = "MIN_DEPOSIT_AMOUNT", Value = "1000", Description = "Số tiền nạp tối thiểu (VNĐ)", CreatedAt = now },
                new SystemConfig { Id = 10, Key = "PRICE_UPDATE_COOLDOWN_DAYS", Value = "7", Description = "Khoảng cách giữa các lần cập nhật giá (ngày)", CreatedAt = now },
                new SystemConfig { Id = 11, Key = "AGORA_TOKEN_EXPIRATION_HOURS", Value = "1", Description = "Thời gian hết hạn token Agora (giờ)", CreatedAt = now },
                new SystemConfig { Id = 12, Key = "NOTIFICATION_PAGE_SIZE", Value = "20", Description = "Kích thước trang thông báo", CreatedAt = now },
                new SystemConfig { Id = 13, Key = "SUBSCRIPTION_PRO_MAX_LIMIT", Value = "7300", Description = "Giới hạn tối đa gói Pro", CreatedAt = now },
                new SystemConfig { Id = 14, Key = "SUBSCRIPTION_PRO_LIMIT_MULTIPLIER", Value = "20", Description = "Hệ số giới hạn gói Pro / ngày", CreatedAt = now },
                new SystemConfig { Id = 15, Key = "SUBSCRIPTION_BASIC_LIMIT", Value = "3", Description = "Giới hạn gói Basic", CreatedAt = now },
                new SystemConfig { Id = 16, Key = "SUBSCRIPTION_RUSH_LIMIT", Value = "20", Description = "Giới hạn gói Rush", CreatedAt = now },
                new SystemConfig { Id = 17, Key = "INTERVIEW_COST_POINTS", Value = "2000", Description = "Chi phí điểm phỏng vấn", CreatedAt = now },
                new SystemConfig { Id = 18, Key = "PRACTICE_QUESTION_COST_POINTS", Value = "1000", Description = "Chi phí điểm câu hỏi luyện tập", CreatedAt = now },
                new SystemConfig { Id = 19, Key = "GUARANTEE_DEPOSIT_RATE", Value = "20.0", Description = "Tỉ lệ tiền đảm bảo (%)", CreatedAt = now }
            );
        }
    }
}
