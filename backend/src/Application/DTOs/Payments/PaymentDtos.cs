namespace Application.DTOs.Payments;

/// <summary>One document a payment is being applied to. Money figures are settled server-side.</summary>
public record AllocationRequest(Guid DocumentId, decimal Amount);

public record ChequeRequest(
    string ChequeNumber,
    string BankName,
    DateOnly ChequeDate,
    DateOnly? ReceivedOn);

/// <summary>
/// <paramref name="AutoAllocateOldestFirst"/> with an empty <paramref name="Allocations"/> settles the
/// party's open documents oldest first — the shop's own model of "against my account".
/// </summary>
public record CreatePaymentRequest(
    string Direction,
    Guid? CustomerId,
    Guid? SupplierId,
    string? WalkInName,
    DateOnly PaymentDate,
    decimal Amount,
    string Mode,
    string? ReferenceNumber,
    string? Notes,
    ChequeRequest? Cheque,
    IReadOnlyList<AllocationRequest> Allocations,
    bool AutoAllocateOldestFirst);

public record AllocatePaymentRequest(IReadOnlyList<AllocationRequest> Allocations);

public record BounceChequeRequest(DateOnly BouncedOn, string Reason, decimal? ChargeAmount);

public record ChequeStatusRequest(DateOnly? OnDate);

/// <summary>A manual correction to a party's balance — a write-off, a rounding difference settled by hand.</summary>
public record AdjustPartyBalanceRequest(
    Guid? CustomerId,
    Guid? SupplierId,
    decimal Amount,
    string Reason);

public record PaymentAllocationDto(
    Guid Id,
    Guid? InvoiceId,
    Guid? PurchaseId,
    string DocumentNumber,
    DateOnly DocumentDate,
    decimal Amount,
    bool IsReversed);

public record ChequeDto(
    string ChequeNumber,
    string BankName,
    DateOnly ChequeDate,
    DateOnly ReceivedOn,
    string Status,
    DateOnly? DepositedOn,
    DateOnly? ClearedOn,
    DateOnly? BouncedOn,
    string? BounceReason,
    /// <summary>Which statuses this cheque may still move to — drives the register's row actions.</summary>
    IReadOnlyList<string> NextStatuses);

public record PaymentDto(
    Guid Id,
    string? ReceiptNumber,
    string Direction,
    DateOnly PaymentDate,
    Guid? CustomerId,
    Guid? SupplierId,
    string PartyName,
    decimal Amount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    string Mode,
    string? ReferenceNumber,
    string? Notes,
    string Status,
    bool IsCounterPayment,
    ChequeDto? Cheque,
    IReadOnlyList<PaymentAllocationDto> Allocations,
    DateTimeOffset CreatedAt);

/// <summary>Row shape for the list screen — allocations are left out, as on the document lists.</summary>
public record PaymentListItemDto(
    Guid Id,
    string? ReceiptNumber,
    string Direction,
    DateOnly PaymentDate,
    string PartyName,
    decimal Amount,
    decimal UnallocatedAmount,
    string Mode,
    string Status,
    bool IsCounterPayment,
    string? ChequeNumber,
    string? ChequeStatus,
    DateOnly? ChequeDate);

public record PaymentListQuery(
    string? Search,
    string? Direction,
    string? Status,
    string? Mode,
    Guid? CustomerId,
    Guid? SupplierId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    bool? UnallocatedOnly,
    int Page = 1,
    int PageSize = 20);

public record ChequeListQuery(string? Status, DateOnly? FromDate, DateOnly? ToDate, int Page = 1, int PageSize = 20);

/// <summary>
/// Money actually in against paper still settling. Kept apart on purpose: cash in the drawer is not
/// a cheque in the drawer.
/// </summary>
/// <summary>How the money arrived, split by tender. Cash is the only one that moves the drawer.</summary>
public record PaymentModeTotalDto(string Mode, string Label, decimal Received, decimal PaidOut, int Count);

public record PaymentSummaryDto(
    decimal Collected,
    decimal PaidOut,
    decimal NetCash,
    decimal ChequesInHand,
    int ChequesInHandCount,
    int PaymentCount,
    /// <summary>
    /// The whole point of collecting a mode on every payment. "₹51,717 collected" cannot be
    /// reconciled against anything; "₹18,400 cash, ₹22,100 UPI, ₹11,217 transfer" can be counted in
    /// the drawer and matched to a bank statement.
    /// </summary>
    IReadOnlyList<PaymentModeTotalDto> ByMode);

public record DuesSummaryDto(
    decimal TotalReceivable,
    decimal TotalPayable,
    decimal AdvancesHeld,
    int CustomersWithDues,
    int SuppliersWithDues);

public record PartyLedgerEntryDto(
    Guid Id,
    string EntryType,
    decimal Amount,
    decimal BalanceAfter,
    DateOnly EntryDate,
    Guid? ReferenceId,
    string? ReferenceNumber,
    string? Notes);

/// <summary>A statement never starts at zero unless the account did — hence the carried-in balance.</summary>
public record PartyStatementDto(
    Guid PartyId,
    string PartyName,
    decimal OpeningBalance,
    decimal ClosingBalance,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<PartyLedgerEntryDto> Entries,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>An open document, oldest first, for the allocation picker.</summary>
public record OpenDocumentDto(
    Guid Id,
    string DocumentNumber,
    DateOnly DocumentDate,
    DateOnly? DueDate,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    int DaysOld);

/// <summary>
/// What the billing screen needs to warn about a customer without blocking the sale. Kept off
/// <c>CustomerDto</c> so the customer list does not pay for cheque aggregates on every row.
/// </summary>
public record CustomerAccountSummaryDto(
    Guid CustomerId,
    decimal OutstandingBalance,
    decimal CreditLimit,
    int CreditDays,
    decimal AdvanceAmount,
    decimal PendingChequeAmount,
    decimal OverdueAmount,
    DateOnly? OldestUnpaidDate,
    DateOnly? LastBounceDate,
    string? LastBounceChequeNumber);
