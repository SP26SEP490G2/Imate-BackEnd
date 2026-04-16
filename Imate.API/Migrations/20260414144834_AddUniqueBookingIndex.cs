using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBookingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_MentorId",
                table: "Bookings");



            migrationBuilder.Sql(@"
                WITH DuplicateBookings AS (
                    SELECT 
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY MentorId, StartTime 
                            ORDER BY Id ASC
                        ) as row_num
                    FROM Bookings
                    WHERE Status NOT IN ('Cancelled', 'Refunded')
                )
                UPDATE Bookings
                SET Status = 'Cancelled', UpdatedAt = SYSDATETIMEOFFSET()
                WHERE Id IN (
                    SELECT Id FROM DuplicateBookings WHERE row_num > 1
                )
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MentorId_StartTime",
                table: "Bookings",
                columns: new[] { "MentorId", "StartTime" },
                unique: true,
                filter: "[Status] <> 'Cancelled' AND [Status] <> 'Refunded'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_MentorId_StartTime",
                table: "Bookings");



            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MentorId",
                table: "Bookings",
                column: "MentorId");
        }
    }
}
