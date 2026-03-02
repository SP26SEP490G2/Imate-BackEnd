using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class MentorPositionConfiguration : IEntityTypeConfiguration<MentorPosition>
    {
        public void Configure(EntityTypeBuilder<MentorPosition> builder)
        {
            builder.ToTable("MentorPositions");
            builder.HasKey(e => new { e.MentorId, e.PositionId });

            builder.HasOne(e => e.Mentor)
                .WithMany(m => m.MentorPositions)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Position)
                .WithMany(p => p.MentorPositions)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
