using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DebitNoteConfiguration : IEntityTypeConfiguration<DebitNote>
{
    public void Configure(EntityTypeBuilder<DebitNote> builder)
    {
        builder.ToTable("debit_notes", t =>
        {
            // The note's value splits two ways and nowhere else: some reduces the supplier's
            // bill, the rest stands as credit on their account. Moving one half moves the other.
            t.HasCheckConstraint(
                "CK_debit_notes_applied_within_total",
                "\"AppliedToPurchaseAmount\" >= 0 AND \"AppliedToPurchaseAmount\" <= \"GrandTotal\"");

            // Only the part that did not go against the bill can come back as cash — the rest was
            // never money that changed hands, it was set against what the shop still owed.
            t.HasCheckConstraint(
                "CK_debit_notes_refund_within_credit",
                "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"GrandTotal\" - \"AppliedToPurchaseAmount\"");
        });

        builder.HasKey(n => n.Id);

        // Same reasoning as PurchaseConfiguration: two returns against one bill at the same moment
        // become a retryable conflict rather than a silently wrong DebitAppliedAmount.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(n => n.DebitNoteNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(n => n.DebitNoteNumber).IsUnique();

        builder.Property(n => n.FinancialYear).IsRequired().HasMaxLength(9);

        // The pair, not the formatted string, is what actually guarantees an unbroken series.
        builder.HasIndex(n => new { n.FinancialYear, n.Sequence }).IsUnique();

        builder.Property(n => n.PurchaseNumber).IsRequired().HasMaxLength(30);

        builder.Property(n => n.SupplierName).IsRequired().HasMaxLength(200);
        builder.Property(n => n.SupplierGstin).HasMaxLength(15);
        builder.Property(n => n.SupplierStateCode).HasMaxLength(2);

        // Required on the printed note under Rule 53(1A)(g), and the first thing an auditor asks.
        builder.Property(n => n.Reason).IsRequired().HasMaxLength(500);

        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

        foreach (var money in new[]
                 {
                     nameof(DebitNote.SubTotal), nameof(DebitNote.DiscountAmount),
                     nameof(DebitNote.TaxableAmount), nameof(DebitNote.CgstAmount),
                     nameof(DebitNote.SgstAmount), nameof(DebitNote.IgstAmount),
                     nameof(DebitNote.TotalTax), nameof(DebitNote.RoundOff),
                     nameof(DebitNote.GrandTotal), nameof(DebitNote.AppliedToPurchaseAmount),
                     nameof(DebitNote.RefundedAmount),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        // Restrict everywhere: a bill with a debit note against it must not vanish underneath it.
        builder.HasOne(n => n.Purchase)
            .WithMany()
            .HasForeignKey(n => n.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Supplier)
            .WithMany()
            .HasForeignKey(n => n.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(n => n.Items)
            .WithOne()
            .HasForeignKey(i => i.DebitNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // "What has come back against this bill" is asked by the guard and by the return screen.
        builder.HasIndex(n => n.PurchaseId);
        builder.HasIndex(n => n.NoteDate);
        builder.HasIndex(n => n.Status);
    }
}
