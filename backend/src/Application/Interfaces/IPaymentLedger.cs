using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

/// <summary>Cheque particulars supplied alongside a payment whose mode is <c>Cheque</c>.</summary>
public record ChequeDraft(
    string ChequeNumber,
    string BankName,
    DateOnly ChequeDate,
    DateOnly ReceivedOn);

/// <summary>
/// One document a payment is being applied to. The entity itself, already loaded and tracked —
/// the ledger never fetches, so its caller controls the transaction.
/// </summary>
public record AllocationTarget(
    Invoice? Invoice,
    Purchase? Purchase,
    decimal Amount,
    /// <summary>Set instead of the two above when this is cash refunded against a return.</summary>
    CreditNote? CreditNote = null,
    DebitNote? DebitNote = null);

/// <summary>Everything needed to record one payment. Party entities are tracked; ids alone would not do.</summary>
public record PaymentDraft(
    PaymentDirection Direction,
    Customer? Customer,
    Supplier? Supplier,
    string PartyName,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMode Mode,
    string? ReferenceNumber,
    string? Notes,
    bool IsCounterPayment,
    ChequeDraft? Cheque,
    IReadOnlyList<AllocationTarget> Allocations);

/// <summary>
/// Orchestrates a whole payment: numbers it, writes its allocations, moves the documents it settles,
/// and records one party-ledger entry for the total.
/// <para>
/// <b>This is the only place in the codebase that assigns <see cref="Invoice.AmountPaid"/>,
/// <see cref="Invoice.BalanceDue"/>, <see cref="Invoice.CreditAppliedAmount"/> or their purchase
/// equivalents.</b> Anything else that moved them would leave the party balance and the documents
/// disagreeing. Returns move a bill too, which is why they come through here rather than through a
/// ledger of their own.
/// </para>
/// <para>
/// Nothing here saves. The caller saves once, so a payment, its allocations, the bills it settles and
/// the ledger row all commit together or not at all.
/// </para>
/// </summary>
public interface IPaymentLedger
{
    /// <summary>
    /// Records a payment walked in with. A post-dated cheque is recorded as
    /// <see cref="PaymentStatus.Pending"/> and settles nothing until it is banked — see
    /// <see cref="PostAsync"/>.
    /// </summary>
    Task<Payment> ReceiveAsync(PaymentDraft draft, CancellationToken cancellationToken);

    /// <summary>
    /// The tender collected while raising a document. Carries no receipt number — the document the
    /// customer is handed <em>is</em> the receipt.
    /// </summary>
    Task<Payment> RecordCounterPaymentAsync(PaymentDraft draft, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a payment that has been sitting <see cref="PaymentStatus.Pending"/> — a post-dated
    /// cheque now taken to the bank. Its documents move and the ledger entry is written now, not on
    /// the day it was handed over.
    /// </summary>
    Task PostAsync(Payment payment, DateOnly effectiveDate, CancellationToken cancellationToken);

    /// <summary>
    /// Sets a return's value against the bill it came from, up to what that bill still had
    /// outstanding, and returns how much it actually absorbed.
    /// <para>
    /// The cap is the point. A bill already settled in full absorbs nothing; the whole credit then
    /// belongs on the party's account, where the caller puts it. Letting the balance go negative
    /// instead would hide the money behind every <c>BalanceDue &gt; 0</c> query and double-count it
    /// against the advance the party ledger is already showing.
    /// </para>
    /// </summary>
    decimal ApplyCredit(Invoice invoice, decimal creditAmount);

    /// <summary>Puts back what <see cref="ApplyCredit"/> took, when a credit note is cancelled.</summary>
    void ReleaseCredit(Invoice invoice, decimal creditAmount);

    /// <summary>The purchase-side mirror, moved by debit notes.</summary>
    decimal ApplyDebit(Purchase purchase, decimal debitAmount);

    void ReleaseDebit(Purchase purchase, decimal debitAmount);

    /// <summary>
    /// Spends an advance: attaches further allocations to a payment that still has unallocated money.
    /// </summary>
    Task AllocateAsync(
        Payment payment, IReadOnlyList<AllocationTarget> targets, CancellationToken cancellationToken);

    /// <summary>
    /// Takes a whole payment back — cancelled, or a cheque returned. Every live allocation is
    /// released, the documents go back to unpaid, and one compensating entry is written.
    /// <para>
    /// <paramref name="entryType"/> is what separates the two cases on the statement:
    /// <see cref="PartyLedgerEntryType.PaymentCancelled"/> means it was keyed in error, while
    /// <see cref="PartyLedgerEntryType.ChequeBounced"/> means it really happened and then failed.
    /// </para>
    /// </summary>
    Task ReverseAsync(
        Payment payment,
        PartyLedgerEntryType entryType,
        DateOnly onDate,
        string reason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases every live allocation pointing at one invoice, without touching the payments
    /// themselves — the customer really did hand the money over, so it becomes an advance on their
    /// account rather than vanishing with the cancelled bill.
    /// </summary>
    Task ReleaseAllocationsForInvoiceAsync(
        Invoice invoice, IReadOnlyList<PaymentAllocation> allocations, CancellationToken cancellationToken);

    /// <summary>The purchase equivalent.</summary>
    Task ReleaseAllocationsForPurchaseAsync(
        Purchase purchase, IReadOnlyList<PaymentAllocation> allocations, CancellationToken cancellationToken);
}
