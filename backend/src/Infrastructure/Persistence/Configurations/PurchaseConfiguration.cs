using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        // See the note on InvoiceConfiguration.
        builder.ToTable("purchases", t =>
        {
            t.HasCheckConstraint(
                "CK_purchases_balance_due",
                "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 " +
                "ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" - \"DebitAppliedAmount\" END");

            t.HasCheckConstraint(
                "CK_purchases_debit_applied",
                "\"DebitAppliedAmount\" >= 0 AND \"DebitAppliedAmount\" <= \"GrandTotal\"");
        });
        builder.HasKey(p => p.Id);

        // Postgres keeps a row version on every table already, so this costs no schema change.
        // It turns two people settling the same bill at once from a silent wrong number
        // into a concurrency failure the caller can retry.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(p => p.PurchaseNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => p.PurchaseNumber).IsUnique();

        builder.Property(p => p.FinancialYear).IsRequired().HasMaxLength(9);

        // The pair, not the formatted string, is what actually guarantees the series has no gaps
        // or repeats — the number is only a rendering of it.
        builder.HasIndex(p => new { p.FinancialYear, p.Sequence }).IsUnique();

        builder.Property(p => p.SupplierInvoiceNumber).IsRequired().HasMaxLength(50);

        // Backs the duplicate-bill check in PurchaseService.
        builder.HasIndex(p => new { p.SupplierId, p.SupplierInvoiceNumber });

        builder.Property(p => p.SupplierName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.SupplierGstin).HasMaxLength(15);
        builder.Property(p => p.SupplierStateCode).HasMaxLength(2);

        builder.Property(p => p.Notes).HasMaxLength(1000);

        // Stored as text: a filed document's payment mode and status must stay readable in the
        // database years later, without a lookup table of integers.
        builder.Property(p => p.PaymentMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        foreach (var money in new[]
                 {
                     nameof(Purchase.SubTotal), nameof(Purchase.DiscountAmount), nameof(Purchase.TaxableAmount),
                     nameof(Purchase.CgstAmount), nameof(Purchase.SgstAmount), nameof(Purchase.IgstAmount),
                     nameof(Purchase.TotalTax), nameof(Purchase.RoundOff), nameof(Purchase.GrandTotal),
                     nameof(Purchase.AmountPaid), nameof(Purchase.BalanceDue),
                     nameof(Purchase.DebitAppliedAmount),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mirrors the invoice side: most bills are settled, so the open list is a small slice.
        builder.HasIndex(p => p.BalanceDue).HasFilter("\"BalanceDue\" > 0");

        builder.HasIndex(p => p.InvoiceDate);
        builder.HasIndex(p => p.Status);
    }
}
