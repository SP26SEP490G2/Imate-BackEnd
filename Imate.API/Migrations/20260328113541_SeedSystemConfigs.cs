using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Id", "CreatedAt", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Tỷ lệ hoa hồng (%)", "COMMISSION_RATE", null, "20.0" },
                    { 2, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Số lượt phỏng vấn miễn phí mặc định", "FREE_INTERVIEW_LIMIT", null, "3" },
                    { 3, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Điểm thưởng khi đóng góp câu hỏi", "CONTRIBUTION_REWARD_POINTS", null, "1000" },
                    { 4, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Thời gian khóa tiền sau khi hoàn thành (giờ)", "ESCROW_HOURS", null, "24" },
                    { 5, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Thời gian đặt lịch trước tối thiểu (giờ)", "MIN_BOOKING_ADVANCE_HOURS", null, "6" },
                    { 6, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Thời hạn hủy phạt (giờ trước khi bắt đầu)", "CANCELLATION_DEADLINE_HOURS", null, "24" },
                    { 7, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Thời hạn khiếu nại sau khi hoàn thành (giờ)", "REPORT_DEADLINE_HOURS", null, "24" },
                    { 8, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Tỷ lệ hoàn tiền khi hủy chậm (%)", "CANCELLATION_REFUND_RATE", null, "80" },
                    { 9, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Số tiền nạp tối thiểu (VNĐ)", "MIN_DEPOSIT_AMOUNT", null, "1000" },
                    { 10, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Khoảng cách giữa các lần cập nhật giá (ngày)", "PRICE_UPDATE_COOLDOWN_DAYS", null, "7" },
                    { 11, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Thời gian hết hạn token Agora (giờ)", "AGORA_TOKEN_EXPIRATION_HOURS", null, "1" },
                    { 12, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Kích thước trang thông báo", "NOTIFICATION_PAGE_SIZE", null, "20" },
                    { 13, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Giới hạn tối đa gói Pro", "SUBSCRIPTION_PRO_MAX_LIMIT", null, "7300" },
                    { 14, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số giới hạn gói Pro / ngày", "SUBSCRIPTION_PRO_LIMIT_MULTIPLIER", null, "20" },
                    { 15, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Giới hạn gói Basic", "SUBSCRIPTION_BASIC_LIMIT", null, "3" },
                    { 16, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Giới hạn gói Rush", "SUBSCRIPTION_RUSH_LIMIT", null, "20" },
                    { 17, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Chi phí điểm phỏng vấn", "INTERVIEW_COST_POINTS", null, "2000" },
                    { 18, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Chi phí điểm câu hỏi luyện tập", "PRACTICE_QUESTION_COST_POINTS", null, "1000" },
                    { 19, new DateTimeOffset(new DateTime(2026, 3, 28, 11, 35, 39, 950, DateTimeKind.Unspecified).AddTicks(2456), new TimeSpan(0, 0, 0, 0, 0)), "Tỉ lệ tiền đảm bảo (%)", "GUARANTEE_DEPOSIT_RATE", null, "20.0" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 19);
        }
    }
}
