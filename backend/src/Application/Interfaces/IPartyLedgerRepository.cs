using Domain.Entities;

namespace Application.Interfaces;

public interface IPartyLedgerRepository
{
    Task AddEntryAsync(PartyLedgerEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// A party's statement over a date range, oldest first, plus the balance carried into the range
    /// — a statement that starts at zero when the account did not is worse than no statement.
    /// <para>
    /// <c>RangeMovement</c> is the sum over the <b>whole</b> range, not the page. The closing figure
    /// has to be the account's, and reading it off the last row returned would make page 1 of a
    /// three-page statement claim a balance the customer does not owe.
    /// </para>
    /// <para>
    /// <c>CarriedIn</c> is the balance standing immediately before this page's first row, and it is
    /// what the running-balance column counts up from. It exists because <c>BalanceAfter</c> cannot
    /// serve as that column: it is stamped in the order rows are <i>written</i>, while a statement
    /// reads in the order things <i>happened</i>. Bank a post-dated cheque and then record a bounce
    /// dated last week, and the stored figures stop adding up down the page.
    /// </para>
    /// </summary>
    Task<(IReadOnlyList<PartyLedgerEntry> Items, int TotalCount, decimal OpeningBalance,
            decimal RangeMovement, decimal CarriedIn)>
        GetStatementAsync(
            Guid? customerId,
            Guid? supplierId,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

    /// <summary>
    /// Sum of a party's entries, straight from the ledger. Used to reconcile the denormalised
    /// balance rather than to serve screens.
    /// </summary>
    Task<decimal> SumForPartyAsync(Guid? customerId, Guid? supplierId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
