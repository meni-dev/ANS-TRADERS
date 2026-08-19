using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class PartyLedgerEntryConfiguration : IEntityTypeConfiguration<PartyLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PartyLedgerEntry> builder)
    {
        builder.ToTable("party_ledger_entries", t =>
            t.HasCheckConstraint(
                "CK_party_ledger_entries_single_party",
                "num_nonnulls(\"CustomerId\", \"SupplierId\") = 1"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PartyName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(30);
        builder.Property(e => e.Notes).HasMaxLength(500);

        // Stored as text like the stock ledger's movement type: a ledger is read by people long
        // after the code that wrote it, and an integer would need a decoder ring.
        builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(30);

        builder.Property(e => e.Amount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.BalanceAfter).HasColumnType("numeric(18,2)");

        builder.HasOne(e => e.Customer)
            .WithMany()
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // A statement is almost always read as "this party, in date order".
        builder.HasIndex(e => new { e.CustomerId, e.RecordedAt });
        builder.HasIndex(e => new { e.SupplierId, e.RecordedAt });
        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.ReferenceId);
    }
}
