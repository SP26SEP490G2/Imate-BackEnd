using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisDataToUserCv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisData",
                table: "UserCvs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisData",
                table: "UserCvs");
        }
    }
}
