using Application.Common;

namespace UnitTests;

public class FinancialYearTests
{
    [Theory]
    // April opens a new year; March closes the previous one.
    [InlineData("2026-04-01", "2026-27")]
    [InlineData("2026-08-16", "2026-27")]
    [InlineData("2027-03-31", "2026-27")]
    [InlineData("2027-04-01", "2027-28")]
    [InlineData("2026-01-15", "2025-26")]
    // The two-digit suffix has to wrap, not run past 99.
    [InlineData("2099-05-01", "2099-00")]
    public void For_FormatsTheIndianFinancialYear(string date, string expected)
    {
        Assert.Equal(expected, FinancialYear.For(DateOnly.Parse(date)));
    }
}
