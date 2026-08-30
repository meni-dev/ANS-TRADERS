namespace UnitTests;

/// <summary>
/// Which day opening stock belongs on.
/// <para>
/// It used to be the day somebody typed the part in, and the consequence took a while to surface: a
/// shop that keys its catalogue in August and then enters April's bills has every sale sitting
/// before the stock it sold. Three parts in this shop read −2, −12 and −5 through the middle of
/// August, and a valuation at any date in that window was meaningless.
/// </para>
/// </summary>
public class OpeningStockDateTests
{
    /// <summary>The rule the two product services share.</summary>
    private static DateOnly Opening(DateOnly? booksStartFrom, DateOnly today) =>
        booksStartFrom is { } start && start <= today ? start : today;

    [Fact]
    public void Opening_stock_is_dated_when_the_books_begin()
    {
        Assert.Equal(
            new DateOnly(2026, 4, 1),
            Opening(new DateOnly(2026, 4, 1), today: new DateOnly(2026, 8, 20)));
    }

    /// <summary>Nothing better to offer, so the day it was keyed stands.</summary>
    [Fact]
    public void A_shop_that_has_not_said_when_its_books_begin_gets_today()
    {
        Assert.Equal(
            new DateOnly(2026, 8, 20),
            Opening(null, today: new DateOnly(2026, 8, 20)));
    }

    /// <summary>
    /// A start date in the future is a typo, and dating stock into the future would put it beyond
    /// every register that could show it.
    /// </summary>
    [Fact]
    public void A_books_start_in_the_future_is_not_used()
    {
        Assert.Equal(
            new DateOnly(2026, 8, 20),
            Opening(new DateOnly(2027, 4, 1), today: new DateOnly(2026, 8, 20)));
    }

    /// <summary>The boundary: a shop keying its catalogue on the very day its books open.</summary>
    [Fact]
    public void Opening_on_the_start_date_itself_is_the_start_date()
    {
        Assert.Equal(
            new DateOnly(2026, 4, 1),
            Opening(new DateOnly(2026, 4, 1), today: new DateOnly(2026, 4, 1)));
    }
}
