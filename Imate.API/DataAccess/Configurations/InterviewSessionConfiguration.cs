using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class InterviewSessionConfiguration : IEntityTypeConfiguration<InterviewSession>
    {
        public void Configure(EntityTypeBuilder<InterviewSession> builder)
        {
            builder.ToTable("InterviewSessions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.StartTime).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.EndTime).IsRequired(false).HasColumnType("datetimeoffset");
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.OverallFeedback).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.InterviewType).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.PositionName).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.SkillName).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.LevelName).HasColumnType("nvarchar(50)").IsRequired(false);
            builder.Property(e => e.CompanyName).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.JobDescriptionText).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.EstimatedAbility).IsRequired(false);
            builder.Property(e => e.TotalQuestionsAnswered).IsRequired().HasDefaultValue(0);
            builder.Property(e => e.CvContent).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.ExtractedSkillsJson).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Account)
                .WithMany(a => a.InterviewSessions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.UserCv)
                .WithMany(c => c.InterviewSessions)
                .HasForeignKey(e => e.UserCvId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Question)
                .WithMany(q => q.InterviewSessions)
                .HasForeignKey(e => e.QuestionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
