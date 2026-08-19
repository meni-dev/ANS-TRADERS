using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.PartNumber).IsRequired().HasMaxLength(100);
        builder.Property(m => m.ItemName).IsRequired().HasMaxLength(200);

        // Stored as text like the document statuses: a ledger is read by people long after the
        // code that wrote it, and an integer would need a decoder ring.
        builder.Property(m => m.MovementType).HasConversion<string>().HasMaxLength(30);

        builder.Property(m => m.Quantity).HasColumnType("numeric(18,3)");
        builder.Property(m => m.BalanceAfter).HasColumnType("numeric(18,3)");

        builder.Property(m => m.ReferenceNumber).HasMaxLength(30);
        builder.Property(m => m.Notes).HasMaxLength(200);

        // Restrict: a product with history behind it cannot be deleted out from under its ledger.
        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // The ledger is almost always read as "this product, newest first".
        builder.HasIndex(m => new { m.ProductId, m.MovedAt });
        builder.HasIndex(m => m.MovedAt);
        builder.HasIndex(m => m.ReferenceId);
    }
}
