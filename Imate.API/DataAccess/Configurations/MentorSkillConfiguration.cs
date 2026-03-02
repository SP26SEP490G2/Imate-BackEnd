using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class MentorSkillConfiguration : IEntityTypeConfiguration<MentorSkill>
    {
        public void Configure(EntityTypeBuilder<MentorSkill> builder)
        {
            builder.ToTable("MentorSkills");
            builder.HasKey(e => new { e.MentorId, e.SkillId });

            builder.HasOne(e => e.Mentor)
                .WithMany(m => m.MentorSkills)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Skill)
                .WithMany(s => s.MentorSkills)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
