using Application.Services;
using Domain.Entities;

namespace UnitTests;

/// <summary>
/// The three ways a return can land on a bill. These are the scenarios the whole returns design was
/// argued from, so they are pinned by name.
/// </summary>
public class ApplyCreditTests
{
    private static PaymentLedger Ledger() =>
        new(new FakePaymentRepository(), new PartyLedger(new FakePartyLedgerRepository()));

    private static Invoice Billed(decimal total, decimal paid) => new()
    {
        InvoiceNumber = "INV/2026-27/0001",
        GrandTotal = total,
        AmountPaid = paid,
        BalanceDue = total - paid,
    };

    [Fact]
    public void AnUnpaidBillAbsorbsTheWholeReturn()
    {
        var invoice = Billed(10_000m, 0m);

        var applied = Ledger().ApplyCredit(invoice, 3_000m);

        Assert.Equal(3_000m, applied);
        Assert.Equal(3_000m, invoice.CreditAppliedAmount);
        Assert.Equal(7_000m, invoice.BalanceDue);
        Assert.Equal(0m, invoice.AmountPaid);
    }

    /// <remarks>
    /// The bill was settled and stays settled. What the customer is owed is a fresh obligation on
    /// their account, not an un-settling of a closed document — which is why nothing here moves.
    /// </remarks>
    [Fact]
    public void ASettledBillAbsorbsNothingAndItsBalanceStaysAtZero()
    {
        var invoice = Billed(10_000m, 10_000m);

        var applied = Ledger().ApplyCredit(invoice, 3_000m);

        Assert.Equal(0m, applied);
        Assert.Equal(0m, invoice.CreditAppliedAmount);
        Assert.Equal(0m, invoice.BalanceDue);
    }

    /// <remarks>
    /// The case that decides the cap. They owed 6,000 and sent back 8,000 of goods, so the bill
    /// closes and the shop ends up owing them 2,000 — which the caller puts on their account.
    /// </remarks>
    [Fact]
    public void AReturnLargerThanTheBalanceClosesTheBillAndStopsThere()
    {
        var invoice = Billed(10_000m, 4_000m);

        var applied = Ledger().ApplyCredit(invoice, 8_000m);

        Assert.Equal(6_000m, applied);
        Assert.Equal(0m, invoice.BalanceDue);
        Assert.Equal(4_000m, invoice.AmountPaid);
    }

    [Fact]
    public void TheBalanceNeverGoesNegative()
    {
        var invoice = Billed(1_000m, 1_000m);

        Ledger().ApplyCredit(invoice, 5_000m);

        Assert.True(invoice.BalanceDue >= 0);
    }

    /// <remarks>
    /// Without this the concurrency token is never checked on a settled bill, because EF sees no
    /// changed column and emits no UPDATE.
    /// </remarks>
    [Fact]
    public void ApplyingNothingStillTouchesTheRow()
    {
        var invoice = Billed(1_000m, 1_000m);
        invoice.UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var before = invoice.UpdatedAt;

        Ledger().ApplyCredit(invoice, 500m);

        Assert.True(invoice.UpdatedAt > before);
    }

    [Fact]
    public void ReleasingPutsTheBillBackExactly()
    {
        var invoice = Billed(10_000m, 4_000m);
        var ledger = Ledger();

        var applied = ledger.ApplyCredit(invoice, 8_000m);
        ledger.ReleaseCredit(invoice, applied);

        Assert.Equal(0m, invoice.CreditAppliedAmount);
        Assert.Equal(6_000m, invoice.BalanceDue);
    }

    [Fact]
    public void TwoReturnsAgainstOneBillStackUpToItsBalance()
    {
        var invoice = Billed(10_000m, 0m);
        var ledger = Ledger();

        Assert.Equal(4_000m, ledger.ApplyCredit(invoice, 4_000m));
        Assert.Equal(6_000m, ledger.ApplyCredit(invoice, 9_000m));
        Assert.Equal(10_000m, invoice.CreditAppliedAmount);
        Assert.Equal(0m, invoice.BalanceDue);
    }
}
