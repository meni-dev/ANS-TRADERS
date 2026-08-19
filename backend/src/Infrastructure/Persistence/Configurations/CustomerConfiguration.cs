using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);

        // Postgres keeps a row version on every table already, so this costs no schema change.
        // It turns two people settling the same account at once from a silent wrong number
        // into a concurrency failure the caller can retry.
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        builder.Property(c => c.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.Phone).IsUnique();

        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Gstin).HasMaxLength(15);

        builder.Property(c => c.AddressLine1).HasMaxLength(200);
        builder.Property(c => c.AddressLine2).HasMaxLength(200);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.State).HasMaxLength(100);
        builder.Property(c => c.StateCode).HasMaxLength(2);
        builder.Property(c => c.Pincode).HasMaxLength(6);

        builder.Property(c => c.CreditLimit).HasColumnType("numeric(18,2)");
        builder.Property(c => c.OpeningBalance).HasColumnType("numeric(18,2)");
        builder.Property(c => c.OutstandingBalance).HasColumnType("numeric(18,2)");

        builder.HasIndex(c => c.Name);

        // Partial index: GSTIN is optional, and Postgres treats every NULL as distinct, so a
        // plain unique index would allow duplicates only for registered customers.
        builder.HasIndex(c => c.Gstin).IsUnique().HasFilter("\"Gstin\" IS NOT NULL");
    }
}
