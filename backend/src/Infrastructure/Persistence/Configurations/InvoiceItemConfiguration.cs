using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        // A line cannot have more come back than went out. Guarded in the service too, but this
        // is the one that holds when two returns land at the same moment.
        builder.ToTable("invoice_items", t => t.HasCheckConstraint(
            "CK_invoice_items_returned_quantity",
            "\"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\""));
        builder.HasKey(i => i.Id);

        builder.Property(i => i.PartNumber).IsRequired().HasMaxLength(100);
        builder.Property(i => i.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Hsn).IsRequired().HasMaxLength(20);
        builder.Property(i => i.Uqc).IsRequired().HasMaxLength(20);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.ReturnedQuantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.GstRate).HasColumnType("numeric(5,2)");
        builder.Property(i => i.DiscountPercent).HasColumnType("numeric(5,2)");

        foreach (var money in new[]
                 {
                     nameof(InvoiceItem.Rate), nameof(InvoiceItem.CostRate), nameof(InvoiceItem.BillDiscountShare), nameof(InvoiceItem.DiscountAmount),
                     nameof(InvoiceItem.GrossAmount), nameof(InvoiceItem.TaxableAmount),
                     nameof(InvoiceItem.CgstAmount), nameof(InvoiceItem.SgstAmount),
                     nameof(InvoiceItem.IgstAmount), nameof(InvoiceItem.LineTotal),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        // See the note on PurchaseItemConfiguration.
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.InvoiceId);
        builder.HasIndex(i => i.ProductId);
    }
}
