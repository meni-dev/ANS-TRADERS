using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        // A line cannot have more come back than went out. Guarded in the service too, but this
        // is the one that holds when two returns land at the same moment.
        builder.ToTable("purchase_items", t => t.HasCheckConstraint(
            "CK_purchase_items_returned_quantity",
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
                     nameof(PurchaseItem.Rate), nameof(PurchaseItem.DiscountAmount),
                     nameof(PurchaseItem.GrossAmount), nameof(PurchaseItem.TaxableAmount),
                     nameof(PurchaseItem.CgstAmount), nameof(PurchaseItem.SgstAmount),
                     nameof(PurchaseItem.IgstAmount), nameof(PurchaseItem.LineTotal),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        // Restrict, not cascade: deleting a product must not quietly rewrite bills that were
        // already filed against it.
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.PurchaseId);
        builder.HasIndex(i => i.ProductId);
    }
}
