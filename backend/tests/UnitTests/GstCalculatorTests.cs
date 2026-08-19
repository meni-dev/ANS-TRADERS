using Application.Common;

namespace UnitTests;

public class GstCalculatorTests
{
    [Fact]
    public void ComputeLine_SplitsTaxAsCgstAndSgst_ForALocalSupply()
    {
        var line = GstCalculator.ComputeLine(
            quantity: 10, rate: 220, discountPercent: 0, gstRate: 28, isInterState: false);

        Assert.Equal(2200m, line.GrossAmount);
        Assert.Equal(2200m, line.TaxableAmount);
        Assert.Equal(308m, line.CgstAmount);
        Assert.Equal(308m, line.SgstAmount);
        Assert.Equal(0m, line.IgstAmount);
        Assert.Equal(2816m, line.LineTotal);
    }

    [Fact]
    public void ComputeLine_ChargesTheWholeTaxAsIgst_ForAnInterStateSupply()
    {
        var line = GstCalculator.ComputeLine(
            quantity: 10, rate: 220, discountPercent: 0, gstRate: 28, isInterState: true);

        Assert.Equal(616m, line.IgstAmount);
        Assert.Equal(0m, line.CgstAmount);
        Assert.Equal(0m, line.SgstAmount);
        Assert.Equal(2816m, line.LineTotal);
    }

    [Fact]
    public void ComputeLine_TakesDiscountOffBeforeTax()
    {
        var line = GstCalculator.ComputeLine(
            quantity: 12, rate: 220, discountPercent: 5, gstRate: 28, isInterState: true);

        Assert.Equal(2640m, line.GrossAmount);
        Assert.Equal(132m, line.DiscountAmount);
        Assert.Equal(2508m, line.TaxableAmount);
        Assert.Equal(702.24m, line.IgstAmount);
    }

    /// <summary>
    /// The reason SGST is computed as the remainder rather than as its own rounded half: at an odd
    /// paisa of tax, two independently rounded halves come to a paisa more than the tax charged.
    /// </summary>
    [Fact]
    public void ComputeLine_KeepsCgstAndSgstAddingUpToTheTax_AtAnOddPaisa()
    {
        // 33.75 at 12% is ₹4.05 of tax — an odd number of paise.
        var line = GstCalculator.ComputeLine(
            quantity: 1, rate: 33.75m, discountPercent: 0, gstRate: 12, isInterState: false);

        Assert.Equal(4.05m, line.CgstAmount + line.SgstAmount);
        Assert.Equal(2.03m, line.CgstAmount);
        Assert.Equal(2.02m, line.SgstAmount);
    }

    [Fact]
    public void ComputeDocument_RoundsTheTotalToWholeRupees_AndReportsTheDifference()
    {
        var lines = new[]
        {
            GstCalculator.ComputeLine(10, 220, 5, 28, isInterState: true),
            GstCalculator.ComputeLine(25, 95, 0, 18, isInterState: true),
        };

        var totals = GstCalculator.ComputeDocument(lines);

        Assert.Equal(4465m, totals.TaxableAmount);
        Assert.Equal(1012.70m, totals.TotalTax);
        Assert.Equal(5478m, totals.GrandTotal);
        // 5477.70 rounds up, so the round-off is a positive 30 paise added to the bill.
        Assert.Equal(0.30m, totals.RoundOff);
    }

    [Fact]
    public void ComputeDocument_ReportsANegativeRoundOff_WhenTheTotalRoundsDown()
    {
        var lines = new[] { GstCalculator.ComputeLine(4, 320, 0, 28, isInterState: false) };

        var totals = GstCalculator.ComputeDocument(lines);

        Assert.Equal(1638m, totals.GrandTotal);
        Assert.Equal(-0.40m, totals.RoundOff);
    }

    [Fact]
    public void ComputeDocument_OfNoLines_IsAllZeroes()
    {
        var totals = GstCalculator.ComputeDocument([]);

        Assert.Equal(0m, totals.GrandTotal);
        Assert.Equal(0m, totals.TotalTax);
        Assert.Equal(0m, totals.RoundOff);
    }

    [Theory]
    [InlineData("33", "29", true)]
    [InlineData("33", "33", false)]
    [InlineData("33", " 33 ", false)]
    // An unknown state on the other party falls back to a local supply — the safe default for a
    // counter sale to a walk-in with nothing on file.
    [InlineData("33", null, false)]
    [InlineData("33", "", false)]
    [InlineData(null, "29", false)]
    public void IsInterState_ComparesStateCodes(string? seller, string? party, bool expected)
    {
        Assert.Equal(expected, GstCalculator.IsInterState(seller, party));
    }
}
