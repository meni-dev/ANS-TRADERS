using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("payment_allocations", t =>
        {
            // An allocation settles exactly one document. Nullable keys rather than a polymorphic
            // (type, id) pair, so the database can still enforce every reference. Refunds settle a
            // credit or debit note the same way a receipt settles a bill.
            t.HasCheckConstraint(
                "CK_payment_allocations_single_document",
                "num_nonnulls(\"InvoiceId\", \"PurchaseId\", \"CreditNoteId\", \"DebitNoteId\") = 1");

            t.HasCheckConstraint("CK_payment_allocations_amount_positive", "\"Amount\" > 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DocumentNumber).IsRequired().HasMaxLength(30);
        builder.Property(a => a.Amount).HasColumnType("numeric(18,2)");

        // Restrict, not cascade: a document with money against it must not be deletable out from
        // under the payment that settled it.
        builder.HasOne(a => a.Invoice)
            .WithMany()
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Purchase)
            .WithMany()
            .HasForeignKey(a => a.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreditNote)
            .WithMany()
            .HasForeignKey(a => a.CreditNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.DebitNote)
            .WithMany()
            .HasForeignKey(a => a.DebitNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.PaymentId);

        // Cancelling a document looks up its live allocations, so both filtered indexes earn their
        // keep — a released row is never the one being searched for.
        builder.HasIndex(a => a.InvoiceId).HasFilter("\"IsReversed\" = false");
        builder.HasIndex(a => a.PurchaseId).HasFilter("\"IsReversed\" = false");
        builder.HasIndex(a => a.CreditNoteId).HasFilter("\"IsReversed\" = false");
        builder.HasIndex(a => a.DebitNoteId).HasFilter("\"IsReversed\" = false");
    }
}
