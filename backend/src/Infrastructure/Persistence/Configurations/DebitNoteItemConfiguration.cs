using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DebitNoteItemConfiguration : IEntityTypeConfiguration<DebitNoteItem>
{
    public void Configure(EntityTypeBuilder<DebitNoteItem> builder)
    {
        builder.ToTable("debit_note_items", t => t.HasCheckConstraint(
            "CK_debit_note_items_quantity_positive", "\"Quantity\" > 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PartNumber).IsRequired().HasMaxLength(100);
        builder.Property(i => i.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Hsn).IsRequired().HasMaxLength(20);
        builder.Property(i => i.Uqc).IsRequired().HasMaxLength(20);

        builder.Property(i => i.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(i => i.GstRate).HasColumnType("numeric(5,2)");
        builder.Property(i => i.DiscountPercent).HasColumnType("numeric(5,2)");

        foreach (var money in new[]
                 {
                     nameof(DebitNoteItem.Rate), nameof(DebitNoteItem.DiscountAmount),
                     nameof(DebitNoteItem.GrossAmount), nameof(DebitNoteItem.TaxableAmount),
                     nameof(DebitNoteItem.CgstAmount), nameof(DebitNoteItem.SgstAmount),
                     nameof(DebitNoteItem.IgstAmount), nameof(DebitNoteItem.LineTotal),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Backs the over-return guard: "how much of this invoice line has already come back".
        builder.HasIndex(i => i.PurchaseItemId);
    }
}
