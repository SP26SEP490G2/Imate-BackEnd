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

            // Seed data: Mapping Position → Skills phù hợp
            // Positions: 1=Backend, 2=Frontend, 3=Fullstack, 4=Mobile, 5=DevOps, 6=Data Engineer, 7=QA, 8=BA
            // Skills: 1=C#, 2=Java, 3=Python, 4=JavaScript, 5=TypeScript, 6=React, 7=Angular, 8=.NET, 9=SQL, 10=Docker
            builder.HasData(
                // Backend Developer (1) → C#, Java, Python, .NET, SQL, Docker
                new PositionSkill { PositionId = 1, SkillId = 1 },  // C#
                new PositionSkill { PositionId = 1, SkillId = 2 },  // Java
                new PositionSkill { PositionId = 1, SkillId = 3 },  // Python
                new PositionSkill { PositionId = 1, SkillId = 8 },  // .NET
                new PositionSkill { PositionId = 1, SkillId = 9 },  // SQL
                new PositionSkill { PositionId = 1, SkillId = 10 }, // Docker

                // Frontend Developer (2) → JavaScript, TypeScript, React, Angular
                new PositionSkill { PositionId = 2, SkillId = 4 },  // JavaScript
                new PositionSkill { PositionId = 2, SkillId = 5 },  // TypeScript
                new PositionSkill { PositionId = 2, SkillId = 6 },  // React
                new PositionSkill { PositionId = 2, SkillId = 7 },  // Angular

                // Fullstack Developer (3) → C#, Java, JavaScript, TypeScript, React, Angular, .NET, SQL
                new PositionSkill { PositionId = 3, SkillId = 1 },  // C#
                new PositionSkill { PositionId = 3, SkillId = 2 },  // Java
                new PositionSkill { PositionId = 3, SkillId = 4 },  // JavaScript
                new PositionSkill { PositionId = 3, SkillId = 5 },  // TypeScript
                new PositionSkill { PositionId = 3, SkillId = 6 },  // React
                new PositionSkill { PositionId = 3, SkillId = 7 },  // Angular
                new PositionSkill { PositionId = 3, SkillId = 8 },  // .NET
                new PositionSkill { PositionId = 3, SkillId = 9 },  // SQL

                // Mobile Developer (4) → Java, JavaScript, TypeScript, React (React Native)
                new PositionSkill { PositionId = 4, SkillId = 2 },  // Java
                new PositionSkill { PositionId = 4, SkillId = 4 },  // JavaScript
                new PositionSkill { PositionId = 4, SkillId = 5 },  // TypeScript
                new PositionSkill { PositionId = 4, SkillId = 6 },  // React (React Native)

                // DevOps Engineer (5) → Python, Docker, SQL
                new PositionSkill { PositionId = 5, SkillId = 3 },  // Python
                new PositionSkill { PositionId = 5, SkillId = 9 },  // SQL
                new PositionSkill { PositionId = 5, SkillId = 10 }, // Docker

                // Data Engineer (6) → Python, SQL
                new PositionSkill { PositionId = 6, SkillId = 3 },  // Python
                new PositionSkill { PositionId = 6, SkillId = 9 },  // SQL

                // QA Engineer (7) → Java, Python, JavaScript, SQL
                new PositionSkill { PositionId = 7, SkillId = 2 },  // Java
                new PositionSkill { PositionId = 7, SkillId = 3 },  // Python
                new PositionSkill { PositionId = 7, SkillId = 4 },  // JavaScript
                new PositionSkill { PositionId = 7, SkillId = 9 },  // SQL

                // Business Analyst (8) → SQL
                new PositionSkill { PositionId = 8, SkillId = 9 }   // SQL
            );
        }
    }
}
