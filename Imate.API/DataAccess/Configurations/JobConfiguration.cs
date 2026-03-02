using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("Jobs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.Title).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.JobDescription).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(e => e.EmploymentType).IsRequired().HasColumnType("nvarchar(100)");
            builder.Property(e => e.Location).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.MinSalary).IsRequired().HasColumnType("bigint");
            builder.Property(e => e.MaxSalary).IsRequired().HasColumnType("bigint");
            builder.Property(e => e.ApplicationDeadline).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.Recruiter)
                .WithMany(a => a.PostedJobs)
                .HasForeignKey(e => e.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
