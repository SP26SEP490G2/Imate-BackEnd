using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedMasterSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM MentorRecurringSlots;"); // Clear existing mentor slots to avoid FK issues
            migrationBuilder.Sql("DELETE FROM Slots;");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Slots', RESEED, 0);");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Slots] ON;
                INSERT INTO [Slots] ([Id], [DayOfWeek], [EndTime], [StartTime]) VALUES 
                (1, 0, '09:00:00', '08:00:00'),(2, 0, '10:00:00', '09:00:00'),(3, 0, '11:00:00', '10:00:00'),(4, 0, '12:00:00', '11:00:00'),(5, 0, '13:00:00', '12:00:00'),(6, 0, '14:00:00', '13:00:00'),(7, 0, '15:00:00', '14:00:00'),(8, 0, '16:00:00', '15:00:00'),(9, 0, '17:00:00', '16:00:00'),(10, 0, '18:00:00', '17:00:00'),(11, 0, '19:00:00', '18:00:00'),(12, 0, '20:00:00', '19:00:00'),(13, 0, '21:00:00', '20:00:00'),(14, 0, '22:00:00', '21:00:00'),
                (15, 1, '09:00:00', '08:00:00'),(16, 1, '10:00:00', '09:00:00'),(17, 1, '11:00:00', '10:00:00'),(18, 1, '12:00:00', '11:00:00'),(19, 1, '13:00:00', '12:00:00'),(20, 1, '14:00:00', '13:00:00'),(21, 1, '15:00:00', '14:00:00'),(22, 1, '16:00:00', '15:00:00'),(23, 1, '17:00:00', '16:00:00'),(24, 1, '18:00:00', '17:00:00'),(25, 1, '19:00:00', '18:00:00'),(26, 1, '20:00:00', '19:00:00'),(27, 1, '21:00:00', '20:00:00'),(28, 1, '22:00:00', '21:00:00'),
                (29, 2, '09:00:00', '08:00:00'),(30, 2, '10:00:00', '09:00:00'),(31, 2, '11:00:00', '10:00:00'),(32, 2, '12:00:00', '11:00:00'),(33, 2, '13:00:00', '12:00:00'),(34, 2, '14:00:00', '13:00:00'),(35, 2, '15:00:00', '14:00:00'),(36, 2, '16:00:00', '15:00:00'),(37, 2, '17:00:00', '16:00:00'),(38, 2, '18:00:00', '17:00:00'),(39, 2, '19:00:00', '18:00:00'),(40, 2, '20:00:00', '19:00:00'),(41, 2, '21:00:00', '20:00:00'),(42, 2, '22:00:00', '21:00:00'),
                (43, 3, '09:00:00', '08:00:00'),(44, 3, '10:00:00', '09:00:00'),(45, 3, '11:00:00', '10:00:00'),(46, 3, '12:00:00', '11:00:00'),(47, 3, '13:00:00', '12:00:00'),(48, 3, '14:00:00', '13:00:00'),(49, 3, '15:00:00', '14:00:00'),(50, 3, '16:00:00', '15:00:00'),(51, 3, '17:00:00', '16:00:00'),(52, 3, '18:00:00', '17:00:00'),(53, 3, '19:00:00', '18:00:00'),(54, 3, '20:00:00', '19:00:00'),(55, 3, '21:00:00', '20:00:00'),(56, 3, '22:00:00', '21:00:00'),
                (57, 4, '09:00:00', '08:00:00'),(58, 4, '10:00:00', '09:00:00'),(59, 4, '11:00:00', '10:00:00'),(60, 4, '12:00:00', '11:00:00'),(61, 4, '13:00:00', '12:00:00'),(62, 4, '14:00:00', '13:00:00'),(63, 4, '15:00:00', '14:00:00'),(64, 4, '16:00:00', '15:00:00'),(65, 4, '17:00:00', '16:00:00'),(66, 4, '18:00:00', '17:00:00'),(67, 4, '19:00:00', '18:00:00'),(68, 4, '20:00:00', '19:00:00'),(69, 4, '21:00:00', '20:00:00'),(70, 4, '22:00:00', '21:00:00'),
                (71, 5, '09:00:00', '08:00:00'),(72, 5, '10:00:00', '09:00:00'),(73, 5, '11:00:00', '10:00:00'),(74, 5, '12:00:00', '11:00:00'),(75, 5, '13:00:00', '12:00:00'),(76, 5, '14:00:00', '13:00:00'),(77, 5, '15:00:00', '14:00:00'),(78, 5, '16:00:00', '15:00:00'),(79, 5, '17:00:00', '16:00:00'),(80, 5, '18:00:00', '17:00:00'),(81, 5, '19:00:00', '18:00:00'),(82, 5, '20:00:00', '19:00:00'),(83, 5, '21:00:00', '20:00:00'),(84, 5, '22:00:00', '21:00:00'),
                (85, 6, '09:00:00', '08:00:00'),(86, 6, '10:00:00', '09:00:00'),(87, 6, '11:00:00', '10:00:00'),(88, 6, '12:00:00', '11:00:00'),(89, 6, '13:00:00', '12:00:00'),(90, 6, '14:00:00', '13:00:00'),(91, 6, '15:00:00', '14:00:00'),(92, 6, '16:00:00', '15:00:00'),(93, 6, '17:00:00', '16:00:00'),(94, 6, '18:00:00', '17:00:00'),(95, 6, '19:00:00', '18:00:00'),(96, 6, '20:00:00', '19:00:00'),(97, 6, '21:00:00', '20:00:00'),(98, 6, '22:00:00', '21:00:00');
                SET IDENTITY_INSERT [Slots] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Slots",
                keyColumn: "Id",
                keyValue: 98);
        }
    }
}
