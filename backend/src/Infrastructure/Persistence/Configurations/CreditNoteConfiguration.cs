using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("credit_notes", t =>
        {
            // The note's value splits two ways and nowhere else: some reduces the bill, the rest
            // becomes credit on the account. Anything that moves one half must move the other.
            t.HasCheckConstraint(
                "CK_credit_notes_applied_within_total",
                "\"AppliedToInvoiceAmount\" >= 0 AND \"AppliedToInvoiceAmount\" <= \"GrandTotal\"");

            // Only the part that did not go to the bill can come back as cash — the shop never took
            // the rest, it was set against what the customer still owed.
            t.HasCheckConstraint(
                "CK_credit_notes_refund_within_credit",
                "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"GrandTotal\" - \"AppliedToInvoiceAmount\"");
        });

        builder.HasKey(n => n.Id);

        // Same reasoning as InvoiceConfiguration: two returns against one bill at the same moment
        // become a retryable conflict rather than a silently wrong CreditAppliedAmount.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(n => n.CreditNoteNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(n => n.CreditNoteNumber).IsUnique();

        builder.Property(n => n.FinancialYear).IsRequired().HasMaxLength(9);

        // The pair, not the formatted string, is what actually guarantees an unbroken series.
        builder.HasIndex(n => new { n.FinancialYear, n.Sequence }).IsUnique();

        builder.Property(n => n.InvoiceNumber).IsRequired().HasMaxLength(30);

        builder.Property(n => n.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(n => n.CustomerPhone).HasMaxLength(20);
        builder.Property(n => n.CustomerGstin).HasMaxLength(15);
        builder.Property(n => n.CustomerStateCode).HasMaxLength(2);

        // Required on the printed note under Rule 53(1A)(g), and the first thing an auditor asks.
        builder.Property(n => n.Reason).IsRequired().HasMaxLength(500);

        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

        foreach (var money in new[]
                 {
                     nameof(CreditNote.SubTotal), nameof(CreditNote.DiscountAmount),
                     nameof(CreditNote.TaxableAmount), nameof(CreditNote.CgstAmount),
                     nameof(CreditNote.SgstAmount), nameof(CreditNote.IgstAmount),
                     nameof(CreditNote.TotalTax), nameof(CreditNote.RoundOff),
                     nameof(CreditNote.GrandTotal), nameof(CreditNote.AppliedToInvoiceAmount),
                     nameof(CreditNote.RefundedAmount),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        // Restrict everywhere: a bill with a credit note against it must not vanish underneath it.
        builder.HasOne(n => n.Invoice)
            .WithMany()
            .HasForeignKey(n => n.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Customer)
            .WithMany()
            .HasForeignKey(n => n.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(n => n.Items)
            .WithOne()
            .HasForeignKey(i => i.CreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // "What has come back against this bill" is asked by the guard and by the return screen.
        builder.HasIndex(n => n.InvoiceId);
        builder.HasIndex(n => n.NoteDate);
        builder.HasIndex(n => n.Status);
    }
}
