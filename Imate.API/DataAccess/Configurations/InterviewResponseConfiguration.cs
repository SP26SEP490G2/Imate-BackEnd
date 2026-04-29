using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class InterviewResponseConfiguration : IEntityTypeConfiguration<InterviewResponse>
    {
        public void Configure(EntityTypeBuilder<InterviewResponse> builder)
        {
            builder.ToTable("InterviewResponses");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.TurnNumber).IsRequired();
            builder.Property(e => e.QuestionContent).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.UserAnswer).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.AnswerTimestamp).IsRequired(false).HasColumnType("datetimeoffset");
            builder.Property(e => e.AIFeedback).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.SuggestedAnswer).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.ExpectedBloomLevel).IsRequired(false);
            builder.Property(e => e.DemonstratedBloomLevel).IsRequired(false);
            builder.Property(e => e.BloomScore).IsRequired(false);
            builder.Property(e => e.DifficultyScore).IsRequired(false);
            builder.Property(e => e.CognitiveLoadScore).IsRequired(false);
            builder.Property(e => e.IntrinsicLoad).IsRequired(false);
            builder.Property(e => e.ExtraneousLoad).IsRequired(false);
            builder.Property(e => e.TechnicalDepthScore).IsRequired(false);
            builder.Property(e => e.ProblemSolvingScore).IsRequired(false);
            builder.Property(e => e.CommunicationScore).IsRequired(false);
            builder.Property(e => e.PracticalExperienceScore).IsRequired(false);
            builder.Property(e => e.StarSituationScore).IsRequired(false);
            builder.Property(e => e.StarTaskScore).IsRequired(false);
            builder.Property(e => e.StarActionScore).IsRequired(false);
            builder.Property(e => e.StarResultScore).IsRequired(false);
            builder.Property(e => e.StructuredFeedbackJson).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.ExpectedAnswerOutline).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.Topic).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.InterviewSession)
                .WithMany(s => s.InterviewResponses)
                .HasForeignKey(e => e.InterviewSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
