using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        // Postgres keeps a row version on every table already, so this costs no schema change.
        // It turns two people settling the same item's stock at once from a silent wrong number
        // into a concurrency failure the caller can retry.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(p => p.ItemCode).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PartNumber).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.PartNumber).IsUnique();

        builder.Property(p => p.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.VehicleBrand).HasMaxLength(100);
        builder.Property(p => p.VehicleModel).HasMaxLength(100);

        builder.Property(p => p.Hsn).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Uqc).IsRequired().HasMaxLength(20);

        builder.Property(p => p.GstRate).HasColumnType("numeric(5,2)");

        // By name, not by number — the same rule the statuses follow. Inserting a member into the
        // enum later must not silently reclassify goods that were sold as something else.
        builder.Property(p => p.SupplyType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CgstRate).HasColumnType("numeric(5,2)");
        builder.Property(p => p.SgstRate).HasColumnType("numeric(5,2)");
        builder.Property(p => p.PurchaseRate).HasColumnType("numeric(18,2)");
        builder.Property(p => p.SellingRate).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Mrp).HasColumnType("numeric(18,2)");
        builder.Property(p => p.OpeningStock).HasColumnType("numeric(18,2)");

        // Three decimals, unlike the money columns: parts are sold by the piece here but the same
        // ledger has to cope with items measured in litres or metres.
        builder.Property(p => p.StockOnHand).HasColumnType("numeric(18,3)");
        builder.Property(p => p.ReorderLevel).HasColumnType("numeric(18,3)");

        builder.HasIndex(p => p.ItemName);
        builder.HasIndex(p => new { p.VehicleBrand, p.VehicleModel });
    }
}
