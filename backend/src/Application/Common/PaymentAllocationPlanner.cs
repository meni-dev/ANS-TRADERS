namespace Application.Common;

/// <summary>An open document a payment could be applied to.</summary>
public readonly record struct OpenDocument(Guid DocumentId, DateOnly DocumentDate, decimal Outstanding);

/// <summary>How much of a payment lands on one document.</summary>
public readonly record struct PlannedAllocation(Guid DocumentId, decimal Amount);

/// <summary>
/// Decides which bills a lump sum settles when the user has not said. Oldest first, because that is
/// the shop's own model of "against my account" — nobody pays this week's bill while last month's is
/// open.
/// <para>
/// Pure and total, like <see cref="GstCalculator"/> and <see cref="FinancialYear"/>: no repository,
/// no clock, no exceptions. What it cannot place it simply leaves over, and the caller records that
/// as an advance.
/// </para>
/// </summary>
public static class PaymentAllocationPlanner
{
    /// <summary>
    /// Consumes <paramref name="amount"/> across <paramref name="openDocuments"/>, oldest first.
    /// Documents with nothing outstanding are skipped rather than given zero-value rows.
    /// </summary>
    public static IReadOnlyList<PlannedAllocation> Plan(
        decimal amount, IEnumerable<OpenDocument> openDocuments)
    {
        var plan = new List<PlannedAllocation>();
        var remaining = Round(amount);

        if (remaining <= 0)
        {
            return plan;
        }

        // Date first, then id — an explicit tiebreak, so two bills raised the same day are consumed
        // in the same order every run rather than in whatever order the database handed them over.
        var ordered = openDocuments
            .Where(d => d.Outstanding > 0)
            .OrderBy(d => d.DocumentDate)
            .ThenBy(d => d.DocumentId);

        foreach (var document in ordered)
        {
            if (remaining <= 0)
            {
                break;
            }

            var applied = Math.Min(remaining, Round(document.Outstanding));
            plan.Add(new PlannedAllocation(document.DocumentId, applied));
            remaining -= applied;
        }

        return plan;
    }

    /// <summary>What is left over after a plan — money on account, against no particular bill.</summary>
    public static decimal Unallocated(decimal amount, IEnumerable<PlannedAllocation> plan) =>
        Round(Round(amount) - plan.Sum(p => p.Amount));

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
