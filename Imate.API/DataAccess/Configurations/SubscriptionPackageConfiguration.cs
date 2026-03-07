using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class SubscriptionPackageConfiguration : IEntityTypeConfiguration<SubscriptionPackage>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPackage> builder)
        {
            builder.ToTable("SubscriptionPackages");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();
            builder.Property(e => e.Name).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.Price).IsRequired().HasColumnType("decimal(10,2)");
            builder.Property(e => e.DurationDays).IsRequired(false);
            builder.Property(e => e.Benefits).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.IsRecommended).IsRequired().HasDefaultValue(false);
        }
    }
}
