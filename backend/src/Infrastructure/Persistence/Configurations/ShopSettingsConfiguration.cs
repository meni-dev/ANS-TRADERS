using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ShopSettingsConfiguration : IEntityTypeConfiguration<ShopSettings>
{
    public void Configure(EntityTypeBuilder<ShopSettings> builder)
    {
        builder.ToTable("shop_settings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.LegalName).HasMaxLength(200);
        builder.Property(s => s.Gstin).HasMaxLength(15);

        builder.Property(s => s.StateCode).IsRequired().HasMaxLength(2);
        builder.Property(s => s.State).IsRequired().HasMaxLength(100);

        builder.Property(s => s.AddressLine1).HasMaxLength(200);
        builder.Property(s => s.AddressLine2).HasMaxLength(200);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.Pincode).HasMaxLength(6);
        builder.Property(s => s.Phone).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(200);

        builder.Property(s => s.InvoiceFooter).HasMaxLength(500);
        builder.Property(s => s.BankDetails).HasMaxLength(500);
        builder.Property(s => s.InvoiceTerms).HasMaxLength(1000);

        // Stored as text like the document statuses: a settings row is read by people long after
        // the code that wrote it, and an integer would need a decoder ring.
        builder.Property(s => s.InvoiceTemplate).HasConversion<string>().HasMaxLength(30);
    }
}
