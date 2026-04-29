using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingJourney_Consolidated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingJourneyId",
                table: "InterviewSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "InterviewResponses",
                type: "nvarchar(255)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainingJourneys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    UserCvId = table.Column<int>(type: "int", nullable: false),
                    JobDescriptionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", nullable: true),
                    GapsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingJourneys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingJourneys_UserCvs_UserCvId",
                        column: x => x.UserCvId,
                        principalTable: "UserCvs",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSessions_TrainingJourneyId",
                table: "InterviewSessions",
                column: "TrainingJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingJourneys_UserCvId",
                table: "TrainingJourneys",
                column: "UserCvId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSessions_TrainingJourneys_TrainingJourneyId",
                table: "InterviewSessions",
                column: "TrainingJourneyId",
                principalTable: "TrainingJourneys",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSessions_TrainingJourneys_TrainingJourneyId",
                table: "InterviewSessions");

            migrationBuilder.DropTable(
                name: "TrainingJourneys");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSessions_TrainingJourneyId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "TrainingJourneyId",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "InterviewResponses");

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "SystemConfigs",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTimeOffset(new DateTime(2026, 4, 14, 14, 48, 33, 761, DateTimeKind.Unspecified).AddTicks(304), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
