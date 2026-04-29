using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class AddJourneyPositionSkillLevelCompanyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "TrainingJourneys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LevelName",
                table: "TrainingJourneys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PositionName",
                table: "TrainingJourneys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkillName",
                table: "TrainingJourneys",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "TrainingJourneys");

            migrationBuilder.DropColumn(
                name: "LevelName",
                table: "TrainingJourneys");

            migrationBuilder.DropColumn(
                name: "PositionName",
                table: "TrainingJourneys");

            migrationBuilder.DropColumn(
                name: "SkillName",
                table: "TrainingJourneys");
        }
    }
}
