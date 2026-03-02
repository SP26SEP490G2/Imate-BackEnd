using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class ContributedDetailConfiguration : IEntityTypeConfiguration<ContributedDetail>
    {
        public void Configure(EntityTypeBuilder<ContributedDetail> builder)
        {
            builder.ToTable("ContributedDetails");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.InterviewDate).IsRequired();
            builder.Property(e => e.Level).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Company)
                .WithMany(c => c.ContributedDetails)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
