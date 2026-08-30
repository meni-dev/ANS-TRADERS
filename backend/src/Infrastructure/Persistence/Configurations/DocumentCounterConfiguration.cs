using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DocumentCounterConfiguration : IEntityTypeConfiguration<DocumentCounter>
{
    public void Configure(EntityTypeBuilder<DocumentCounter> builder)
    {
        builder.ToTable("document_counters");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(c => c.FinancialYear).HasMaxLength(10).IsRequired();

        // The unique index is not decoration — it is what ON CONFLICT resolves against, so the
        // whole mechanism depends on it existing.
        builder.HasIndex(c => new { c.Kind, c.FinancialYear }).IsUnique();
    }
}
