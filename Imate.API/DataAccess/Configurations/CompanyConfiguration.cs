using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();
            builder.Property(e => e.Name).IsRequired().HasColumnType("nvarchar(255)");
            builder.HasIndex(e => e.Name).IsUnique();
            builder.Property(e => e.ImageUrl).IsRequired().HasColumnType("nvarchar(500)");
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetime");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetime");
        }
    }
}
