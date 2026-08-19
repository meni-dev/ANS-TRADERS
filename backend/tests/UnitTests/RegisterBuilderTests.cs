using Application.Common;

namespace UnitTests;

public class RegisterBuilderTests
{
    private static RegisterBuilder Sales() =>
        new RegisterBuilder()
            .Date("date", "Date")
            .Text("number", "Invoice No")
            .Money("taxable", "Taxable")
            .Money("rate", "Rate", total: false);

    [Fact]
    public void Cells_land_under_their_own_column_whatever_order_they_are_given_in()
    {
        var builder = Sales();

        // Deliberately out of order. Positional rows are what makes a register readable and also
        // what makes it silently wrong, so the order a caller writes must not matter.
        builder.Row(
            ("taxable", 1000m),
            ("number", "INV/2026-27/0001"),
            ("date", new DateOnly(2026, 8, 19)));

        var register = builder.Build("sales", "Sales", "", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));
        var row = Assert.Single(register.Rows);

        Assert.Equal(["2026-08-19", "INV/2026-27/0001", "1000.00", null], row);
    }

    [Fact]
    public void A_column_that_does_not_exist_is_refused_rather_than_dropped()
    {
        var builder = Sales();

        var error = Assert.Throws<InvalidOperationException>(() => builder.Row(("taxble", 1000m)));

        Assert.Contains("taxble", error.Message);
    }

    [Fact]
    public void Only_columns_asked_to_total_get_one()
    {
        var builder = Sales();
        builder.Row(("taxable", 1000m), ("rate", 18m));
        builder.Row(("taxable", 500m), ("rate", 12m));

        var register = builder.Build("sales", "Sales", "", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        var total = Assert.Single(register.Totals);
        Assert.Equal("taxable", total.ColumnKey);
        Assert.Equal(1500m, total.Value);
    }

    /// <summary>
    /// A register with no rows still prints its total line, reading zero. Leaving the total out
    /// would read as "not calculated" on the one screen whose job is to be trusted.
    /// </summary>
    [Fact]
    public void An_empty_register_totals_to_zero_rather_than_to_nothing()
    {
        var register = Sales()
            .Build("sales", "Sales", "", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Empty(register.Rows);
        Assert.Equal(0, register.RowCount);
        Assert.Equal(0m, Assert.Single(register.Totals).Value);
    }

    /// <summary>
    /// The register is opened in a spreadsheet, sometimes on a machine set to a different locale.
    /// A comma decimal separator would turn every figure into text the accountant cannot sum.
    /// </summary>
    /// <summary>
    /// A register that describes a position rather than a period says so, and the screen then stops
    /// offering a date range that would do nothing.
    /// </summary>
    [Fact]
    public void A_position_register_is_marked_as_at_a_date()
    {
        var asAt = new DateOnly(2026, 8, 19);

        var period = Sales().Build("sales", "Sales", "", asAt, asAt);
        var position = Sales().Build("stock-valuation", "Stock", "", asAt, asAt, isAsAt: true);

        Assert.False(period.IsAsAt);
        Assert.True(position.IsAsAt);
    }

    [Fact]
    public void Figures_are_written_in_invariant_form()
    {
        var builder = Sales();
        builder.Row(("taxable", 1234567.5m));

        var register = builder.Build("sales", "Sales", "", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        Assert.Equal("1234567.50", register.Rows[0][2]);
    }
}
