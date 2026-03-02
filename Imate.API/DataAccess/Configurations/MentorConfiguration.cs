using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class MentorConfiguration : IEntityTypeConfiguration<Mentor>
    {
        public void Configure(EntityTypeBuilder<Mentor> builder)
        {
            builder.ToTable("Mentors");
            builder.HasKey(e => e.AccountId);

            builder.Property(e => e.Bio).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.Phone).IsRequired().HasColumnType("nvarchar(20)");
            builder.Property(e => e.BirthDate).IsRequired(false);
            builder.Property(e => e.Yoe).IsRequired();
            builder.Property(e => e.CvUrl).HasColumnType("nvarchar(500)").IsRequired(false);
            builder.Property(e => e.CertificateUrl).HasColumnType("nvarchar(500)").IsRequired(false);
            builder.Property(e => e.PricePerSession).IsRequired();
            builder.Property(e => e.PriceLastUpdatedDate).IsRequired(false).HasColumnType("datetimeoffset");
            builder.Property(e => e.AvgRatings).IsRequired(false).HasColumnType("decimal(5,2)");
            builder.Property(e => e.TotalRatingCount).IsRequired(false);
            builder.Property(e => e.BankAccountHolderName).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.BankAccountNumber).IsRequired().HasColumnType("nvarchar(50)");
            builder.Property(e => e.BankCode).IsRequired().HasColumnType("nvarchar(50)");
        }
    }
}
