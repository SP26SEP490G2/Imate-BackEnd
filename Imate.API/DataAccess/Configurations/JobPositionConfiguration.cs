using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class JobPositionConfiguration : IEntityTypeConfiguration<JobPosition>
    {
        public void Configure(EntityTypeBuilder<JobPosition> builder)
        {
            builder.ToTable("JobPositions");
            builder.HasKey(e => new { e.JobId, e.PositionId });

            builder.HasOne(e => e.Job)
                .WithMany(j => j.JobPositions)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Position)
                .WithMany(p => p.JobPositions)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
