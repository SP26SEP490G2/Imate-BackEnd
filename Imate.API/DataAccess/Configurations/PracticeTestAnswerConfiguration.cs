using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class PracticeTestAnswerConfiguration : IEntityTypeConfiguration<PracticeTestAnswer>
    {
        public void Configure(EntityTypeBuilder<PracticeTestAnswer> builder)
        {
            builder.ToTable("PracticeTestAnswers");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.QuestionNumber).IsRequired();
            builder.Property(e => e.QuestionText).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.OptionsJson).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.CorrectAnswer).IsRequired().HasColumnType("nvarchar(10)");
            builder.Property(e => e.UserAnswer).IsRequired(false).HasColumnType("nvarchar(10)");
            builder.Property(e => e.IsCorrect).IsRequired();
            builder.Property(e => e.Explanation).HasColumnType("nvarchar(max)").IsRequired(false);

            builder.HasOne(e => e.PracticeTestSession)
                .WithMany(s => s.Answers)
                .HasForeignKey(e => e.PracticeTestSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
