using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class PracticeTestSessionConfiguration : IEntityTypeConfiguration<PracticeTestSession>
    {
        public void Configure(EntityTypeBuilder<PracticeTestSession> builder)
        {
            builder.ToTable("PracticeTestSessions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.TestTitle).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.TestType).IsRequired().HasColumnType("nvarchar(50)");
            builder.Property(e => e.Field).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.Level).IsRequired().HasColumnType("nvarchar(50)");
            builder.Property(e => e.TotalQuestions).IsRequired();
            builder.Property(e => e.CorrectAnswers).IsRequired();
            builder.Property(e => e.Score).IsRequired();
            builder.Property(e => e.TimeLimitMinutes).IsRequired();
            builder.Property(e => e.DurationMinutes).IsRequired(false);
            builder.Property(e => e.TechnicalScore).IsRequired(false);
            builder.Property(e => e.LogicalScore).IsRequired(false);
            builder.Property(e => e.OptimizationScore).IsRequired(false);
            builder.Property(e => e.AiFeedback).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.AiStrengths).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.AiImprovements).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.CompletedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Account)
                .WithMany(a => a.PracticeTestSessions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
