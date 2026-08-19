using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DayCloseConfiguration : IEntityTypeConfiguration<DayClose>
{
    public void Configure(EntityTypeBuilder<DayClose> builder)
    {
        builder.ToTable("day_closes", t => t.HasCheckConstraint(
            // The difference is the whole point of the record; letting it disagree with its own two
            // figures would make the shortage report meaningless.
            "CK_day_closes_difference",
            "\"Difference\" = \"CountedCash\" - \"ExpectedCash\""));

        builder.HasKey(d => d.Id);

        // One close per day. Two would mean two different answers to "what was in the drawer".
        builder.HasIndex(d => d.CloseDate).IsUnique();

        builder.Property(d => d.Reason).HasMaxLength(500);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        foreach (var money in new[]
                 {
                     nameof(DayClose.OpeningCash), nameof(DayClose.CashReceived),
                     nameof(DayClose.CashPaidOut), nameof(DayClose.CashExpenses),
                     nameof(DayClose.ExpectedCash), nameof(DayClose.CountedCash),
                     nameof(DayClose.Difference),
                 })
        {
            builder.Property(money).HasColumnType("numeric(18,2)");
        }
    }
}
