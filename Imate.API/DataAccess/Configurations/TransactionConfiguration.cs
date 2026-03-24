using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).UseIdentityColumn();

            builder.Property(e => e.TransactionType).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.Amount).IsRequired();
            builder.Property(e => e.CommissionRateApplied).IsRequired(false).HasColumnType("decimal(5,2)");
            builder.Property(e => e.Status).IsRequired().HasConversion<string>().HasColumnType("nvarchar(50)");
            builder.Property(e => e.EscrowDeadline).IsRequired(false).HasColumnType("datetimeoffset");
            builder.Property(e => e.ExternalTransactionCode).HasColumnType("nvarchar(255)").IsRequired(false);
            builder.Property(e => e.Reason).HasColumnType("nvarchar(max)").IsRequired(false);
            builder.Property(e => e.CreatedAt).IsRequired().HasColumnType("datetimeoffset");
            builder.Property(e => e.UpdatedAt).IsRequired(false).HasColumnType("datetimeoffset");

            builder.HasOne(e => e.SourceAccount)
                .WithMany(a => a.SourceTransactions)
                .HasForeignKey(e => e.SourceAccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.TargetAccount)
                .WithMany(a => a.TargetTransactions)
                .HasForeignKey(e => e.TargetAccountId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Reviewer)
                .WithMany(a => a.ReviewedTransactions)
                .HasForeignKey(e => e.ReviewerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Booking)
                .WithMany(b => b.Transactions)
                .HasForeignKey(e => e.BookingId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.UserSubscription)
               .WithOne(u => u.Transaction)
               .HasForeignKey<Transaction>(e => e.UserSubscriptionId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Application)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.ApplicationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // 1-1 with WithdrawalDetail
            builder.HasOne(e => e.WithdrawalDetail)
                .WithOne(w => w.Transaction)
                .HasForeignKey<WithdrawalDetail>(w => w.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
