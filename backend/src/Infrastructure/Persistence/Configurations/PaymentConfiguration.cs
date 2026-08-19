using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t =>
        {
            t.HasCheckConstraint("CK_payments_amount_positive", "\"Amount\" > 0");

            // A payment belongs to one party or to nobody. Both set would make the ledger ambiguous
            // about whose balance moved.
            t.HasCheckConstraint(
                "CK_payments_single_party",
                "num_nonnulls(\"CustomerId\", \"SupplierId\") <= 1");

            t.HasCheckConstraint(
                "CK_payments_allocation_adds_up",
                "\"AllocatedAmount\" + \"UnallocatedAmount\" = \"Amount\"");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ReceiptNumber).HasMaxLength(30);

        // Unique only where present: counter payments deliberately carry no number, and Postgres
        // treats every NULL as distinct anyway — the filter makes that explicit rather than accidental.
        builder.HasIndex(p => p.ReceiptNumber).IsUnique().HasFilter("\"ReceiptNumber\" IS NOT NULL");

        builder.Property(p => p.FinancialYear).IsRequired().HasMaxLength(9);

        // The pair, not the formatted string, is what guarantees the receipt series has no gaps or
        // repeats — same rule the invoice and purchase series follow.
        builder.HasIndex(p => new { p.Direction, p.FinancialYear, p.Sequence })
            .IsUnique()
            .HasFilter("\"Sequence\" IS NOT NULL");

        builder.Property(p => p.Direction).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.PartyName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ReferenceNumber).HasMaxLength(60);
        builder.Property(p => p.Notes).HasMaxLength(500);

        foreach (var money in new[]
                 {
                     nameof(Payment.Amount),
                     nameof(Payment.AllocatedAmount),
                     nameof(Payment.UnallocatedAmount),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        builder.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Allocations)
            .WithOne(a => a.Payment!)
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.PaymentDate);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.SupplierId);

        // Most payments are fully applied, so the advances list is a small slice of a big table.
        builder.HasIndex(p => p.UnallocatedAmount).HasFilter("\"UnallocatedAmount\" > 0");
    }
}
