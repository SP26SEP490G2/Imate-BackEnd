using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class PositionSkillConfiguration : IEntityTypeConfiguration<PositionSkill>
    {
        public void Configure(EntityTypeBuilder<PositionSkill> builder)
        {
            builder.ToTable("PositionSkills");
            builder.HasKey(e => new { e.PositionId, e.SkillId });

            builder.HasOne(e => e.Position)
                .WithMany(p => p.PositionSkills)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Skill)
                .WithMany(s => s.PositionSkills)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
