using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ChequeDetailConfiguration : IEntityTypeConfiguration<ChequeDetail>
{
    public void Configure(EntityTypeBuilder<ChequeDetail> builder)
    {
        builder.ToTable("cheque_details");

        // The payment's key is this row's key. That is what makes "one cheque per payment"
        // structural rather than a rule somebody has to remember.
        builder.HasKey(c => c.PaymentId);

        builder.HasOne(c => c.Payment)
            .WithOne(p => p.Cheque!)
            .HasForeignKey<ChequeDetail>(c => c.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.ChequeNumber).IsRequired().HasMaxLength(20);
        builder.Property(c => c.BankName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.BounceReason).HasMaxLength(200);

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        // The register is read as "what is still out there, soonest bankable first".
        builder.HasIndex(c => new { c.Status, c.ChequeDate });

        // Banks reuse numbers across customers, so this is for lookup, not uniqueness.
        builder.HasIndex(c => c.ChequeNumber);
    }
}
