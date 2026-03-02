using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imate.API.DataAccess.Configurations
{
    public class WithdrawalDetailConfiguration : IEntityTypeConfiguration<WithdrawalDetail>
    {
        public void Configure(EntityTypeBuilder<WithdrawalDetail> builder)
        {
            builder.ToTable("WithdrawalDetails");
            builder.HasKey(e => e.TransactionId);

            builder.Property(e => e.BankCode).IsRequired().HasColumnType("nvarchar(50)");
            builder.Property(e => e.BankAccountHolder).IsRequired().HasColumnType("nvarchar(255)");
            builder.Property(e => e.BankAccountNumber).IsRequired().HasColumnType("nvarchar(50)");
        }
    }
}
