using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.Name)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)");

            builder.HasIndex(e => e.Name).IsUnique();
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            builder.HasData(
                new Role { Id = 1, Name = RoleName.Candidate, CreatedAt = seedDate },
                new Role { Id = 2, Name = RoleName.Mentor, CreatedAt = seedDate },
                new Role { Id = 3, Name = RoleName.Recruiter, CreatedAt = seedDate },
                new Role { Id = 4, Name = RoleName.Staff, CreatedAt = seedDate },
                new Role { Id = 5, Name = RoleName.Admin, CreatedAt = seedDate }
            );
        }
    }
}
