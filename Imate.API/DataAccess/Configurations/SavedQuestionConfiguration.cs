using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class SavedQuestionConfiguration : IEntityTypeConfiguration<SavedQuestion>
    {
        public void Configure(EntityTypeBuilder<SavedQuestion> builder)
        {
            builder.ToTable("SavedQuestions");
            builder.HasKey(e => new { e.AccountId, e.QuestionId });

            builder.HasOne(e => e.Account)
                .WithMany(a => a.SavedQuestions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Question)
                .WithMany(q => q.SavedQuestions)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
