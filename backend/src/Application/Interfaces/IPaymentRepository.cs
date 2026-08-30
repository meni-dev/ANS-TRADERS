using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IPaymentRepository
{
    /// <summary>Loads a payment with its allocations and cheque row, tracked so it can be moved.</summary>
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchAsync(
        string? search,
        PaymentDirection? direction,
        PaymentStatus? status,
        PaymentMode? mode,
        Guid? customerId,
        Guid? supplierId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? unallocatedOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Cheques by status, for the register. Ordered by cheque date — that is when it can be banked.</summary>
    Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchChequesAsync(
        ChequeStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Highest sequence used in a financial year for one direction, or 0. Standalone receipts only —
    /// counter payments carry no number and must not consume the series.
    /// </summary>

    /// <summary>
    /// Every live allocation pointing at one document, tracked. Used when a document is cancelled and
    /// its money has to be released.
    /// </summary>
    Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForInvoiceAsync(
        Guid invoiceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForPurchaseAsync(
        Guid purchaseId, CancellationToken cancellationToken);

    /// <summary>Refunds already handed back against a return, for when that return is cancelled.</summary>
    Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForCreditNoteAsync(
        Guid creditNoteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForDebitNoteAsync(
        Guid debitNoteId, CancellationToken cancellationToken);

    /// <summary>
    /// A party's open documents, oldest first and <b>tracked</b> — the auto-allocate path mutates
    /// them, and the paged search deliberately does not track.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetOpenInvoicesForCustomerAsync(
        Guid customerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Purchase>> GetOpenPurchasesForSupplierAsync(
        Guid supplierId, CancellationToken cancellationToken);

    /// <summary>
    /// The same open documents as above, read-only and shaped for the allocation picker. Separate
    /// from the tracked pair on purpose: a screen that only displays rows should not load them into
    /// the change tracker, where an accidental write would be saved by the next unrelated commit.
    /// </summary>
    Task<IReadOnlyList<DTOs.Payments.OpenDocumentDto>> GetOpenDocumentsAsync(
        Guid? customerId, Guid? supplierId, DateOnly asOf, CancellationToken cancellationToken);

    /// <summary>
    /// Everything the billing screen needs to warn about a customer, in one round trip. Returns a
    /// DTO rather than entities for the reason <see cref="IDashboardRepository"/> gives: these are
    /// aggregates across four tables with no single row behind them.
    /// </summary>
    Task<DTOs.Payments.CustomerAccountSummaryDto?> GetCustomerAccountSummaryAsync(
        Guid customerId, DateOnly asOf, CancellationToken cancellationToken);

    Task AddAsync(Payment payment, CancellationToken cancellationToken);

    /// <summary>
    /// Adds one allocation explicitly rather than relying on it being reached through
    /// <c>Payment.Allocations</c>.
    /// <para>
    /// <see cref="Domain.Common.Entity"/> hands out its <c>Id</c> in the initialiser, so a row added
    /// to an <b>already-tracked</b> payment fails EF's "the key is still default, so this must be
    /// new" test and is staged as an UPDATE against a row that does not exist. That is invisible
    /// until a payment is allocated after the request that created it — which is exactly what
    /// banking a post-dated cheque does.
    /// </para>
    /// </summary>
    void AddAllocation(PaymentAllocation allocation);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
