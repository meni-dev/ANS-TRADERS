using Domain;
using Domain.Enums;

namespace UnitTests;

public class ChequeTransitionTests
{
    [Theory]
    [InlineData(ChequeStatus.Pending, ChequeStatus.Deposited)]
    [InlineData(ChequeStatus.Pending, ChequeStatus.Cancelled)]
    [InlineData(ChequeStatus.Deposited, ChequeStatus.Cleared)]
    [InlineData(ChequeStatus.Deposited, ChequeStatus.Bounced)]
    public void IsAllowed_PermitsTheNormalRoute(ChequeStatus from, ChequeStatus to)
    {
        Assert.True(ChequeTransitions.IsAllowed(from, to));
    }

    /// <summary>
    /// The shop banked it and never recorded the deposit. Refusing would be pedantic about
    /// bookkeeping the user did not do.
    /// </summary>
    [Fact]
    public void IsAllowed_PermitsPendingStraightToBounced()
    {
        Assert.True(ChequeTransitions.IsAllowed(ChequeStatus.Pending, ChequeStatus.Bounced));
    }

    [Fact]
    public void IsAllowed_PermitsDepositedBackToPending_ToUndoAMisclick()
    {
        Assert.True(ChequeTransitions.IsAllowed(ChequeStatus.Deposited, ChequeStatus.Pending));
    }

    [Theory]
    [InlineData(ChequeStatus.Cleared)]
    [InlineData(ChequeStatus.Bounced)]
    [InlineData(ChequeStatus.Cancelled)]
    public void IsAllowed_RefusesEveryMoveOutOfATerminalStatus(ChequeStatus terminal)
    {
        foreach (var to in Enum.GetValues<ChequeStatus>())
        {
            Assert.False(ChequeTransitions.IsAllowed(terminal, to));
        }
    }

    /// <summary>
    /// A bounced cheque is never re-presented in place — that is a fresh payment, because one that
    /// bounces twice has to appear on the statement twice.
    /// </summary>
    [Fact]
    public void IsAllowed_RefusesReopeningABouncedCheque()
    {
        Assert.False(ChequeTransitions.IsAllowed(ChequeStatus.Bounced, ChequeStatus.Pending));
        Assert.False(ChequeTransitions.IsAllowed(ChequeStatus.Bounced, ChequeStatus.Deposited));
    }

    [Fact]
    public void IsAllowed_RefusesSkippingStraightFromPendingToCleared()
    {
        // Money is not in the bank until it has been to the bank.
        Assert.False(ChequeTransitions.IsAllowed(ChequeStatus.Pending, ChequeStatus.Cleared));
    }

    [Fact]
    public void IsAllowed_RefusesStayingPut()
    {
        foreach (var status in Enum.GetValues<ChequeStatus>())
        {
            Assert.False(ChequeTransitions.IsAllowed(status, status));
        }
    }

    /// <summary>Every pair is either explicitly allowed or refused — no status falls off the table.</summary>
    [Fact]
    public void EveryStatusPairIsAccountedFor()
    {
        var statuses = Enum.GetValues<ChequeStatus>();
        var allowed = 0;

        foreach (var from in statuses)
        {
            foreach (var to in statuses)
            {
                if (ChequeTransitions.IsAllowed(from, to)) allowed++;
            }
        }

        // Pending → Deposited/Bounced/Cancelled, Deposited → Cleared/Bounced/Pending.
        Assert.Equal(6, allowed);
        Assert.Equal(25, statuses.Length * statuses.Length);
    }

    [Fact]
    public void IsTerminal_IsTrueForExactlyTheThreeEndStates()
    {
        Assert.True(ChequeTransitions.IsTerminal(ChequeStatus.Cleared));
        Assert.True(ChequeTransitions.IsTerminal(ChequeStatus.Bounced));
        Assert.True(ChequeTransitions.IsTerminal(ChequeStatus.Cancelled));

        Assert.False(ChequeTransitions.IsTerminal(ChequeStatus.Pending));
        Assert.False(ChequeTransitions.IsTerminal(ChequeStatus.Deposited));
    }

    [Fact]
    public void IsOutstanding_CoversWhatTheShopOrTheBankStillHolds()
    {
        Assert.True(ChequeTransitions.IsOutstanding(ChequeStatus.Pending));
        Assert.True(ChequeTransitions.IsOutstanding(ChequeStatus.Deposited));

        Assert.False(ChequeTransitions.IsOutstanding(ChequeStatus.Cleared));
        Assert.False(ChequeTransitions.IsOutstanding(ChequeStatus.Bounced));
        Assert.False(ChequeTransitions.IsOutstanding(ChequeStatus.Cancelled));
    }
}
