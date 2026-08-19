using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// One line of a party's account: an append-only record of every movement, with the document behind
/// it. <see cref="Customer.OutstandingBalance"/> is the running total of these rows — kept on the
/// party so a screen can read a balance without summing a ledger, and reconcilable against it at any
/// time.
/// <para>
/// This is the money-side twin of <see cref="StockMovement"/>, and it exists for the same reason:
/// "why does Ramesh owe ₹4,300?" is answered with a dated statement, never with a number.
/// </para>
/// </summary>
public class PartyLedgerEntry : Entity
{
    /// <summary>Exactly one of this and <see cref="SupplierId"/> is set.</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Snapshot, so the statement still reads correctly after a party is renamed.</summary>
    public string PartyName { get; set; } = string.Empty;

    public PartyLedgerEntryType EntryType { get; set; }

    /// <summary>
    /// Signed. Positive increases what is open on this account in the direction natural to the party
    /// — a customer owing more, or the shop owing a supplier more. One signed column rather than an
    /// amount plus a direction flag, so a balance is a plain SUM that cannot disagree with itself.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>Balance immediately after this entry. Makes the statement readable on its own.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// The business date — the day the shop would say this happened. A day's collections filter on
    /// this, never on <see cref="RecordedAt"/>: a six-in-the-morning receipt is the previous day in
    /// UTC, and a statement that reorders itself across a timezone boundary is a support call.
    /// </summary>
    public DateOnly EntryDate { get; set; }

    /// <summary>System time. What the statement orders by, so same-day rows keep the order they happened.</summary>
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>The invoice, purchase or payment behind this. Null for an opening balance.</summary>
    public Guid? ReferenceId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}
