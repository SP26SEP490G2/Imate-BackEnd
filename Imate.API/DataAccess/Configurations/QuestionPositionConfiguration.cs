using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class QuestionPositionConfiguration : IEntityTypeConfiguration<QuestionPosition>
    {
        public void Configure(EntityTypeBuilder<QuestionPosition> builder)
        {
            builder.ToTable("QuestionPositions");
            builder.HasKey(e => new { e.QuestionId, e.PositionId });

            builder.HasOne(e => e.Question)
                .WithMany(q => q.QuestionPositions)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Position)
                .WithMany(p => p.QuestionPositions)
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
