using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeTestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeTestSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    TestTitle = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    TestType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    TechnicalScore = table.Column<int>(type: "int", nullable: true),
                    LogicalScore = table.Column<int>(type: "int", nullable: true),
                    OptimizationScore = table.Column<int>(type: "int", nullable: true),
                    AiFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiStrengths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiImprovements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeTestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeTestSessions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PracticeTestAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PracticeTestSessionId = table.Column<int>(type: "int", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    UserAnswer = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeTestAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeTestAnswers_PracticeTestSessions_PracticeTestSessionId",
                        column: x => x.PracticeTestSessionId,
                        principalTable: "PracticeTestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTestAnswers_PracticeTestSessionId",
                table: "PracticeTestAnswers",
                column: "PracticeTestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTestSessions_AccountId",
                table: "PracticeTestSessions",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeTestAnswers");

            migrationBuilder.DropTable(
                name: "PracticeTestSessions");
        }
    }
}
