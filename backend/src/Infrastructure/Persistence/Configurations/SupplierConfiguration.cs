using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(s => s.Id);

        // Postgres keeps a row version on every table already, so this costs no schema change.
        // It turns two people settling the same account at once from a silent wrong number
        // into a concurrency failure the caller can retry.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);

        builder.Property(s => s.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(s => s.Phone).IsUnique();

        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Gstin).HasMaxLength(15);
        builder.Property(s => s.ContactPerson).HasMaxLength(200);

        builder.Property(s => s.AddressLine1).HasMaxLength(200);
        builder.Property(s => s.AddressLine2).HasMaxLength(200);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.State).HasMaxLength(100);
        builder.Property(s => s.StateCode).HasMaxLength(2);
        builder.Property(s => s.Pincode).HasMaxLength(6);
        builder.Property(s => s.PaymentTerms).HasMaxLength(100);

        builder.Property(s => s.OpeningBalance).HasColumnType("numeric(18,2)");
        builder.Property(s => s.OutstandingBalance).HasColumnType("numeric(18,2)");

        builder.HasIndex(s => s.Name);

        // Partial index: GSTIN is optional, and Postgres treats every NULL as distinct, so a
        // plain unique index would allow duplicates only for registered suppliers.
        builder.HasIndex(s => s.Gstin).IsUnique().HasFilter("\"Gstin\" IS NOT NULL");
    }
}
