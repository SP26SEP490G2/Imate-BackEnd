using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedMaxDepositAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Id", "CreatedAt", "Description", "Key", "UpdatedAt", "Value" },
                values: new object[]
                {
                    21,
                    DateTimeOffset.UtcNow,
                    "Số tiền nạp tối đa (VNĐ)",
                    "MAX_DEPOSIT_AMOUNT",
                    null,
                    "5000000"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
            table: "SystemConfigs",
            keyColumn: "Id",
            keyValue: 21);
        }
    }
}
