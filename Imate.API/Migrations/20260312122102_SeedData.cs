using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Imate.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ApprovalStatus",
                table: "Questions",
                type: "nvarchar(50)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Seed data using raw SQL with IF NOT EXISTS to avoid PK conflicts
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Categories] ON;
                IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [Id] = 1) INSERT INTO [Categories] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (1,'Behavioral',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [Id] = 2) INSERT INTO [Categories] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (2,'Technical',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [Id] = 3) INSERT INTO [Categories] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (3,'System Design',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [Id] = 4) INSERT INTO [Categories] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (4,'Coding',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Categories] WHERE [Id] = 5) INSERT INTO [Categories] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (5,'Case Study',1,'2024-01-01');
                SET IDENTITY_INSERT [Categories] OFF;
            ");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Positions] ON;
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 1) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (1,'Backend Developer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 2) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (2,'Frontend Developer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 3) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (3,'Fullstack Developer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 4) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (4,'Mobile Developer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 5) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (5,'DevOps Engineer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 6) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (6,'Data Engineer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 7) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (7,'QA Engineer',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Positions] WHERE [Id] = 8) INSERT INTO [Positions] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (8,'Business Analyst',1,'2024-01-01');
                SET IDENTITY_INSERT [Positions] OFF;
            ");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Roles] ON;
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 1) INSERT INTO [Roles] ([Id],[Name],[CreatedAt]) VALUES (1,'Candidate','2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 2) INSERT INTO [Roles] ([Id],[Name],[CreatedAt]) VALUES (2,'Mentor','2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 3) INSERT INTO [Roles] ([Id],[Name],[CreatedAt]) VALUES (3,'Recruiter','2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 4) INSERT INTO [Roles] ([Id],[Name],[CreatedAt]) VALUES (4,'Staff','2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 5) INSERT INTO [Roles] ([Id],[Name],[CreatedAt]) VALUES (5,'Admin','2024-01-01');
                SET IDENTITY_INSERT [Roles] OFF;
            ");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Skills] ON;
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 1)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (1,'C#',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 2)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (2,'Java',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 3)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (3,'Python',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 4)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (4,'JavaScript',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 5)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (5,'TypeScript',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 6)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (6,'React',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 7)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (7,'Angular',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 8)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (8,'.NET',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 9)  INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (9,'SQL',1,'2024-01-01');
                IF NOT EXISTS (SELECT 1 FROM [Skills] WHERE [Id] = 10) INSERT INTO [Skills] ([Id],[Name],[IsActive],[CreatedAt]) VALUES (10,'Docker',1,'2024-01-01');
                SET IDENTITY_INSERT [Skills] OFF;
            ");

            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [SubscriptionPackages] ON;
                IF NOT EXISTS (SELECT 1 FROM [SubscriptionPackages] WHERE [Id] = 1) INSERT INTO [SubscriptionPackages] ([Id],[Name],[Price],[DurationDays],[IsActive],[Benefits]) VALUES (1,'Free',0,NULL,1,'{""features"":[""1 mock interview per month"",""Basic resume feedback""]}');
                IF NOT EXISTS (SELECT 1 FROM [SubscriptionPackages] WHERE [Id] = 2) INSERT INTO [SubscriptionPackages] ([Id],[Name],[Price],[DurationDays],[IsActive],[IsRecommended],[Benefits]) VALUES (2,'Premium',199000,30,1,1,'{""features"":[""Unlimited mock interviews"",""AI career assistant"",""Detailed feedback reports""]}');
                IF NOT EXISTS (SELECT 1 FROM [SubscriptionPackages] WHERE [Id] = 3) INSERT INTO [SubscriptionPackages] ([Id],[Name],[Price],[DurationDays],[IsActive],[Benefits]) VALUES (3,'Enterprise',499000,90,1,'{""features"":[""All Premium features"",""1-on-1 expert coaching session"",""Priority support""]}');
                SET IDENTITY_INSERT [SubscriptionPackages] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ApprovalStatus",
                table: "Questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);
        }
    }
}
