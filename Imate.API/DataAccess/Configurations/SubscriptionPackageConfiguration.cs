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
            builder.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(e => e.DurationDays).IsRequired(false);
            builder.Property(e => e.Benefits).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.IsRecommended).IsRequired().HasDefaultValue(false);

            builder.HasData(
                new SubscriptionPackage { Id = 1, Name = "Free", Price = 0, DurationDays = null, 
                    Benefits = "{\"features\":[\"1 mock interview per month\",\"Basic resume feedback\"]}", 
                    IsActive = true, IsRecommended = false },
                new SubscriptionPackage { Id = 2, Name = "Premium", Price = 199000, DurationDays = 30, 
                    Benefits = "{\"features\":[\"Unlimited mock interviews\",\"AI career assistant\",\"Detailed feedback reports\"]}", 
                    IsActive = true, IsRecommended = true },
                new SubscriptionPackage { Id = 3, Name = "Enterprise", Price = 499000, DurationDays = 90, 
                    Benefits = "{\"features\":[\"All Premium features\",\"1-on-1 expert coaching session\",\"Priority support\"]}", 
                    IsActive = true, IsRecommended = false }
            );
        }
    }
}
