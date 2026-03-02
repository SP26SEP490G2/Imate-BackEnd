using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.Email).IsRequired().HasColumnType("nvarchar(255)");
            builder.HasIndex(e => e.Email).IsUnique();

            builder.Property(e => e.FullName).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.AvatarUrl).HasColumnType("nvarchar(500)").IsRequired(false);

            builder.Property(e => e.Provider)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)");

            builder.Property(e => e.ProviderId).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.Balance).IsRequired().HasDefaultValue(0);

            builder.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasColumnType("nvarchar(50)");

            builder.Property(e => e.FreeUsedMock).IsRequired().HasDefaultValue(0);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            // 1-1 with Mentor
            builder.HasOne(e => e.Mentor)
                .WithOne(m => m.Account)
                .HasForeignKey<Mentor>(m => m.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1-1 with Recruiter
            builder.HasOne(e => e.Recruiter)
                .WithOne(r => r.Account)
                .HasForeignKey<Recruiter>(r => r.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
