using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", t => t.HasCheckConstraint(
            "CK_expenses_amount_positive", "\"Amount\" > 0"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExpenseNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(e => e.ExpenseNumber).IsUnique();

        builder.Property(e => e.FinancialYear).IsRequired().HasMaxLength(9);

        // The pair, not the formatted string, is what guarantees the series has no gaps.
        builder.HasIndex(e => new { e.FinancialYear, e.Sequence }).IsUnique();

        // Stored as text so a filed year's expenses stay readable without a lookup table.
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Mode).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.Amount).HasColumnType("numeric(18,2)");

        builder.Property(e => e.ReferenceNumber).HasMaxLength(50);
        builder.Property(e => e.PaidTo).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        // The two questions asked of this table: what did I spend this month, and on what.
        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => e.Category);
    }
}
