using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class SkillConfiguration : IEntityTypeConfiguration<Skill>
    {
        public void Configure(EntityTypeBuilder<Skill> builder)
        {
            builder.ToTable("Skills");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();
            builder.Property(e => e.Name).IsRequired().HasColumnType("nvarchar(255)");
            builder.HasIndex(e => e.Name).IsUnique();
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            builder.HasData(
                new Skill { Id = 1, Name = "C#", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 2, Name = "Java", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 3, Name = "Python", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 4, Name = "JavaScript", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 5, Name = "TypeScript", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 6, Name = "React", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 7, Name = "Angular", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 8, Name = ".NET", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 9, Name = "SQL", IsActive = true, CreatedAt = seedDate },
                new Skill { Id = 10, Name = "Docker", IsActive = true, CreatedAt = seedDate }
            );
        }
    }
}
