using Imate.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class TrainingJourneyConfiguration : IEntityTypeConfiguration<TrainingJourney>
    {
        public void Configure(EntityTypeBuilder<TrainingJourney> builder)
        {
            builder.ToTable("TrainingJourneys");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.JobDescriptionText).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.GapsJson).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.Name).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired().HasColumnType("datetimeoffset");

            builder.HasOne(e => e.UserCv)
                .WithMany()
                .HasForeignKey(e => e.UserCvId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(j => j.Sessions)
                .WithOne(s => s.TrainingJourney)
                .HasForeignKey(s => s.TrainingJourneyId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
