using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("Questions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.Difficulty).IsRequired(false).HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.ApprovalStatus).IsRequired(false).HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.IsFromSystem).IsRequired();
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.SampleAnswer).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Creator)
                .WithMany(a => a.Questions)
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.ContributedDetail)
                .WithMany(c => c.Questions)
                .HasForeignKey(e => e.ContributedDetailId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
