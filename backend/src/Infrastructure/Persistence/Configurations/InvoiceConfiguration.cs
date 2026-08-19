using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        // A cancelled document owes nothing, so its balance is zero whatever it once collected.
        // AmountPaid is deliberately left alone: money really was taken across the counter, and
        // erasing that to satisfy a constraint would destroy the only record of it.
        //
        // Three terms, not two: goods can come back as well as money going out, and both reduce
        // what is still owed. Extending the identity rather than relaxing it keeps the database the
        // thing that guarantees it — no code path can move one column and forget another.
        builder.ToTable("invoices", t =>
        {
            t.HasCheckConstraint(
                "CK_invoices_balance_due",
                "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 " +
                "ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" - \"CreditAppliedAmount\" END");

            // Cheap, and it makes an over-credit unrepresentable rather than merely guarded.
            t.HasCheckConstraint(
                "CK_invoices_credit_applied",
                "\"CreditAppliedAmount\" >= 0 AND \"CreditAppliedAmount\" <= \"GrandTotal\"");
        });
        builder.HasKey(i => i.Id);

        // Postgres keeps a row version on every table already, so this costs no schema change.
        // It turns two people settling the same invoice at once from a silent wrong number
        // into a concurrency failure the caller can retry.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.FinancialYear).IsRequired().HasMaxLength(9);

        // See the note on PurchaseConfiguration: the pair is the real guarantee of an unbroken series.
        builder.HasIndex(i => new { i.FinancialYear, i.Sequence }).IsUnique();

        builder.Property(i => i.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.CustomerPhone).HasMaxLength(20);
        builder.Property(i => i.CustomerGstin).HasMaxLength(15);
        builder.Property(i => i.CustomerStateCode).HasMaxLength(2);

        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.Property(i => i.PaymentMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        foreach (var money in new[]
                 {
                     nameof(Invoice.BillDiscountPercent), nameof(Invoice.BillDiscountAmount), nameof(Invoice.SubTotal), nameof(Invoice.DiscountAmount), nameof(Invoice.TaxableAmount),
                     nameof(Invoice.CgstAmount), nameof(Invoice.SgstAmount), nameof(Invoice.IgstAmount),
                     nameof(Invoice.TotalTax), nameof(Invoice.RoundOff), nameof(Invoice.GrandTotal),
                     nameof(Invoice.AmountPaid), nameof(Invoice.BalanceDue),
                     nameof(Invoice.CreditAppliedAmount),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        // Optional: a walk-in sale has no customer row behind it.
        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.InvoiceDate);
        builder.HasIndex(i => i.Status);

        // The outstanding-dues screen filters on this, and most rows are fully paid.
        builder.HasIndex(i => i.BalanceDue).HasFilter("\"BalanceDue\" > 0");
    }
}
