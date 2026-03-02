using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class QuestionSkillConfiguration : IEntityTypeConfiguration<QuestionSkill>
    {
        public void Configure(EntityTypeBuilder<QuestionSkill> builder)
        {
            builder.ToTable("QuestionSkills");
            builder.HasKey(e => new { e.QuestionId, e.SkillId });

            builder.HasOne(e => e.Question)
                .WithMany(q => q.QuestionSkills)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Skill)
                .WithMany(s => s.QuestionSkills)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
