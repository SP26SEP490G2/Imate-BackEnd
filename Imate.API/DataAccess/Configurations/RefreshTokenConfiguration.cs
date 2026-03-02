using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.Token).IsRequired().HasColumnType("nvarchar(500)");
            builder.HasIndex(e => e.Token).IsUnique();
            builder.Property(e => e.ExpiresAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.RevokedAt).IsRequired(false).HasColumnType("datetimeoffset");
            builder.Property(e => e.IpAddress).HasColumnType("nvarchar(50)").IsRequired(false);
            builder.Property(e => e.UserAgent).HasColumnType("nvarchar(500)").IsRequired(false);

            builder.HasOne(e => e.Account)
                .WithMany(a => a.RefreshTokens)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
