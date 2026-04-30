using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class Add_Deposit_And_Withdrawal_Config : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
            table: "SystemConfigs",
            columns: new[] { "Key", "Value", "Description", "CreatedAt" },
            values: new object[,]
            {
                {
                    "DEPOSIT_TIMEOUT_MINUTES",
                    "5",
                    "Thời gian timeout thanh toán nạp tiền (phút)",
                    DateTime.UtcNow
                },
                {
                    "WITHDRAWAL_AUTO_REFUND_HOURS",
                    "48",
                    "Thời gian tự động hoàn tiền nếu chưa xử lý (giờ)",
                    DateTime.UtcNow
                }
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
            table: "SystemConfigs",
            keyColumn: "Key",
            keyValues: new object[]
            {
                "DEPOSIT_TIMEOUT_MINUTES",
                "WITHDRAWAL_AUTO_REFUND_HOURS"
            });
        }
    }
}
