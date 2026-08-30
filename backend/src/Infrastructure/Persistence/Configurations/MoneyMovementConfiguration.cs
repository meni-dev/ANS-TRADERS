using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class MoneyMovementConfiguration : IEntityTypeConfiguration<MoneyMovement>
{
    public void Configure(EntityTypeBuilder<MoneyMovement> builder)
    {
        builder.ToTable("money_movements", t => t.HasCheckConstraint(
            "CK_money_movements_amount_positive", "\"Amount\" > 0"));

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Kind).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(m => m.Amount).HasColumnType("numeric(18,2)");
        builder.Property(m => m.ReferenceNumber).HasMaxLength(60);
        builder.Property(m => m.Notes).HasMaxLength(500);

        // The cash book reads these by date on every load of the screen.
        builder.HasIndex(m => m.MovementDate);
    }
}
