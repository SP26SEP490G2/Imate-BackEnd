using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserSubscriptionTransactionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserSubscriptionId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserSubscriptionId",
                table: "Transactions",
                column: "UserSubscriptionId",
                unique: true,
                filter: "[UserSubscriptionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserSubscriptionId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserSubscriptionId",
                table: "Transactions",
                column: "UserSubscriptionId");
        }
    }
}
