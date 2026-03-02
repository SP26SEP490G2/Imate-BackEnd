using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
    {
        public void Configure(EntityTypeBuilder<JobApplication> builder)
        {
            builder.ToTable("JobApplications");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.AppliedDate).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.RecruiterFeedback).HasColumnType("nvarchar(max)").IsRequired(false);

            builder.HasOne(e => e.Job)
                .WithMany(j => j.JobApplications)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Candidate)
                .WithMany(a => a.CandidateJobApplications)
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Cv)
                .WithMany(c => c.JobApplications)
                .HasForeignKey(e => e.CvId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
