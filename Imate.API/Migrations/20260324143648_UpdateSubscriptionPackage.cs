using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyInterviewLimit",
                table: "SubscriptionPackages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "SubscriptionPackages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalInterviewLimit",
                table: "SubscriptionPackages",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SubscriptionPackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DailyInterviewLimit", "Rank", "TotalInterviewLimit" },
                values: new object[] { null, 0, null });

            migrationBuilder.UpdateData(
                table: "SubscriptionPackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DailyInterviewLimit", "Rank", "TotalInterviewLimit" },
                values: new object[] { null, 0, null });

            migrationBuilder.UpdateData(
                table: "SubscriptionPackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DailyInterviewLimit", "Rank", "TotalInterviewLimit" },
                values: new object[] { null, 0, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyInterviewLimit",
                table: "SubscriptionPackages");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "SubscriptionPackages");

            migrationBuilder.DropColumn(
                name: "TotalInterviewLimit",
                table: "SubscriptionPackages");
        }
    }
}
