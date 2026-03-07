using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();
            builder.Property(e => e.Name).IsRequired().HasColumnType("nvarchar(255)");
            builder.HasIndex(e => e.Name).IsUnique();
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            var seedDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            builder.HasData(
                new Category { Id = 1, Name = "Behavioral", IsActive = true, CreatedAt = seedDate },
                new Category { Id = 2, Name = "Technical", IsActive = true, CreatedAt = seedDate },
                new Category { Id = 3, Name = "System Design", IsActive = true, CreatedAt = seedDate },
                new Category { Id = 4, Name = "Coding", IsActive = true, CreatedAt = seedDate },
                new Category { Id = 5, Name = "Case Study", IsActive = true, CreatedAt = seedDate }
            );
        }
    }
}
