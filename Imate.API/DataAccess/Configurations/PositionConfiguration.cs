using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class PositionConfiguration : IEntityTypeConfiguration<Position>
    {
        public void Configure(EntityTypeBuilder<Position> builder)
        {
            builder.ToTable("Positions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();
            builder.Property(e => e.Name).IsRequired().HasColumnType("nvarchar(255)");
            builder.HasIndex(e => e.Name).IsUnique();
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            builder.HasData(
                new Position { Id = 1, Name = "Backend Developer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 2, Name = "Frontend Developer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 3, Name = "Fullstack Developer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 4, Name = "Mobile Developer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 5, Name = "DevOps Engineer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 6, Name = "Data Engineer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 7, Name = "QA Engineer", IsActive = true, CreatedAt = seedDate },
                new Position { Id = 8, Name = "Business Analyst", IsActive = true, CreatedAt = seedDate }
            );
        }
    }
}
