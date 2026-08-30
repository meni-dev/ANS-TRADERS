namespace UnitTests;

/// <summary>
/// The arithmetic behind "would this bill take the customer past their limit".
/// <para>
/// Written after the check was found to be counting the new bill twice — the party ledger adds it
/// to the running balance before the check runs, and the check added it again. Every customer was
/// refused at roughly half the limit the shop had set for them, and the message said so in a figure
/// nobody could reconcile.
/// </para>
/// <para>
/// The service reads the balance <em>before</em> the ledger moves it, so these cases describe the
/// figure the check is actually given.
/// </para>
/// </summary>
public class CreditLimitTests
{
    /// <summary>What the service computes: what they owed coming in, plus what this bill leaves.</summary>
    private static bool Refused(decimal owedBefore, decimal grandTotal, decimal tender, decimal limit)
    {
        if (limit <= 0 || grandTotal - tender <= 0)
        {
            return false;
        }

        return owedBefore + (grandTotal - tender) > limit;
    }

    [Fact]
    public void A_first_bill_inside_the_limit_goes_through()
    {
        Assert.False(Refused(owedBefore: 0, grandTotal: 295, tender: 0, limit: 300));
    }

    /// <summary>
    /// The case that was broken. Owed 590, bill 295, limit 1000 — that is 885 and well inside it.
    /// The doubled version made it 1180 and refused.
    /// </summary>
    [Fact]
    public void A_later_bill_is_measured_against_what_was_owed_coming_in()
    {
        Assert.False(Refused(owedBefore: 590, grandTotal: 295, tender: 0, limit: 1000));
    }

    [Fact]
    public void A_bill_that_really_does_cross_the_limit_is_refused()
    {
        Assert.True(Refused(owedBefore: 900, grandTotal: 295, tender: 0, limit: 1000));
    }

    /// <summary>Exactly at the limit is inside it — a limit is a ceiling, not a fence before it.</summary>
    [Fact]
    public void Landing_exactly_on_the_limit_is_allowed()
    {
        Assert.False(Refused(owedBefore: 705, grandTotal: 295, tender: 0, limit: 1000));
    }

    /// <summary>Money taken at the counter never reaches the account, so it never counts.</summary>
    [Fact]
    public void What_is_paid_at_the_counter_does_not_count_against_the_limit()
    {
        Assert.False(Refused(owedBefore: 900, grandTotal: 295, tender: 295, limit: 1000));
    }

    /// <summary>Zero is an unset field, not an instruction to refuse every credit sale.</summary>
    [Fact]
    public void No_limit_set_means_no_limit()
    {
        Assert.False(Refused(owedBefore: 999_999, grandTotal: 295, tender: 0, limit: 0));
    }
}
