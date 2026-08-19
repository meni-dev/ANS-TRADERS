using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// The only way a party's balance is allowed to change. Invoices, purchases, receipts, cancellations
/// and manual corrections all go through here, so that <see cref="Customer.OutstandingBalance"/> and
/// the ledger can never drift apart — a service that set the balance directly would leave a number
/// nothing could explain.
/// <para>
/// The money-side twin of <see cref="IStockLedger"/>, and it keeps the same contract.
/// </para>
/// </summary>
public interface IPartyLedger
{
    /// <summary>
    /// Applies a signed amount to <paramref name="customer"/> and appends the matching ledger row.
    /// Nothing is persisted: the caller saves, so the entry commits in the same transaction as the
    /// document or payment that caused it.
    /// <para>
    /// Positive increases what the customer owes; negative reduces it, and past zero becomes an
    /// advance the shop is holding.
    /// </para>
    /// </summary>
    Task RecordForCustomerAsync(
        Customer customer,
        decimal signedAmount,
        PartyLedgerEntryType entryType,
        DateOnly entryDate,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken);

    /// <summary>
    /// The supplier equivalent. A separate overload rather than a shared abstraction because
    /// customers and suppliers are separate aggregates here — the same reason
    /// <c>Validators/PartyRules.cs</c> gives for keeping their rules side by side rather than merged.
    /// <para>
    /// Positive increases what the shop owes the supplier.
    /// </para>
    /// </summary>
    Task RecordForSupplierAsync(
        Supplier supplier,
        decimal signedAmount,
        PartyLedgerEntryType entryType,
        DateOnly entryDate,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken);
}
