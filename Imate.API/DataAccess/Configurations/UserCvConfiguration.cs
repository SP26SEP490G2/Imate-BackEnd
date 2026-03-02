using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class UserCvConfiguration : IEntityTypeConfiguration<UserCv>
    {
        public void Configure(EntityTypeBuilder<UserCv> builder)
        {
            builder.ToTable("UserCvs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.FileUrl).IsRequired().HasColumnType("nvarchar(500)");
            builder.Property(e => e.FileName).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.UploadDate).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.ScannedData).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Account)
                .WithMany(a => a.UserCvs)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
