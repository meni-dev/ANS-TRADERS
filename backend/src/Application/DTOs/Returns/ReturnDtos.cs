namespace Application.DTOs.Returns;

/// <summary>
/// One line coming back. <b>Quantity is the only figure the client sends</b> — rate, discount and
/// GST rate are copied from the invoice line the note is reversing.
/// <para>
/// Not a convenience. A credit note has to reverse the tax that was actually charged; taking a rate
/// from the request would let today's price credit last month's sale, and the note would no longer
/// reconcile with the invoice in GSTR-1. It also makes an over-return by value impossible — cap the
/// quantity and the value is capped with it.
/// </para>
/// </summary>
public record ReturnLineRequest(Guid DocumentItemId, decimal Quantity);

public record CreateCreditNoteRequest(
    Guid InvoiceId,
    DateOnly NoteDate,
    string Reason,
    IReadOnlyList<ReturnLineRequest> Lines,
    /// <summary>Cash handed back at the counter now. Null or 0 leaves the credit on account.</summary>
    decimal? RefundAmount,
    string? RefundMode,
    string? RefundReference);

public record CreateDebitNoteRequest(
    Guid PurchaseId,
    DateOnly NoteDate,
    string Reason,
    IReadOnlyList<ReturnLineRequest> Lines,
    decimal? RefundAmount,
    string? RefundMode,
    string? RefundReference);

public record ReturnNoteItemDto(
    Guid Id,
    Guid DocumentItemId,
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string Hsn,
    string Uqc,
    decimal Quantity,
    decimal Rate,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal GrossAmount,
    decimal TaxableAmount,
    decimal GstRate,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal LineTotal);

public record CreditNoteDto(
    Guid Id,
    string CreditNoteNumber,
    string FinancialYear,
    DateOnly NoteDate,
    Guid InvoiceId,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid? CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerGstin,
    string? CustomerStateCode,
    bool IsInterState,
    int ItemCount,
    string Reason,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TotalTax,
    decimal RoundOff,
    decimal GrandTotal,
    /// <summary>How much went against the bill. The rest is credit on the account.</summary>
    decimal AppliedToInvoiceAmount,
    decimal RefundedAmount,
    /// <summary>What is still available to hand back in cash.</summary>
    decimal RefundableAmount,
    string Status,
    IReadOnlyList<ReturnNoteItemDto> Items,
    DateTimeOffset CreatedAt);

public record CreditNoteListItemDto(
    Guid Id,
    string CreditNoteNumber,
    DateOnly NoteDate,
    string InvoiceNumber,
    string CustomerName,
    int ItemCount,
    decimal GrandTotal,
    decimal RefundedAmount,
    string Status);

public record CreditNoteListQuery(
    string? Search,
    Guid? CustomerId,
    Guid? InvoiceId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// What is still returnable on one document, line by line. Its own endpoint rather than part of the
/// document DTO: the bill list would otherwise pay for this aggregate on every row of every page —
/// the same reason the customer account summary is kept off <c>CustomerDto</c>.
/// </summary>
public record ReturnableLineDto(
    Guid DocumentItemId,
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string Uqc,
    decimal QuantitySold,
    decimal QuantityReturned,
    decimal QuantityReturnable,
    decimal Rate,
    decimal DiscountPercent,
    decimal GstRate);

public record ReturnableDocumentDto(
    Guid DocumentId,
    string DocumentNumber,
    DateOnly DocumentDate,
    string PartyName,
    /// <summary>
    /// From the original document. The return screen previews the tax split with it — the total is
    /// the same either way, but showing CGST+SGST beside a note that will say IGST reads as a bug.
    /// </summary>
    bool IsInterState,
    bool CanReturn,
    /// <summary>Why not, when <see cref="CanReturn"/> is false — shown instead of a dead button.</summary>
    string? BlockedReason,
    IReadOnlyList<ReturnableLineDto> Lines);

// ---------------------------------------------------------------------------------------------
// The purchase side. Field for field the same as the sales side with the nouns swapped, exactly as
// Purchase already mirrors Invoice — one shape, so both can share the calculator and the layout.
// ---------------------------------------------------------------------------------------------

public record DebitNoteDto(
    Guid Id,
    string DebitNoteNumber,
    string FinancialYear,
    DateOnly NoteDate,
    Guid PurchaseId,
    string PurchaseNumber,
    DateOnly PurchaseDate,
    Guid? SupplierId,
    string SupplierName,
    string? SupplierPhone,
    string? SupplierGstin,
    string? SupplierStateCode,
    bool IsInterState,
    int ItemCount,
    string Reason,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TotalTax,
    decimal RoundOff,
    decimal GrandTotal,
    /// <summary>How much went against the bill. The rest is credit on the account.</summary>
    decimal AppliedToPurchaseAmount,
    decimal RefundedAmount,
    /// <summary>What is still available to hand back in cash.</summary>
    decimal RefundableAmount,
    string Status,
    IReadOnlyList<ReturnNoteItemDto> Items,
    DateTimeOffset CreatedAt);
public record DebitNoteListItemDto(
    Guid Id,
    string DebitNoteNumber,
    DateOnly NoteDate,
    string PurchaseNumber,
    string SupplierName,
    int ItemCount,
    decimal GrandTotal,
    decimal RefundedAmount,
    string Status);
public record DebitNoteListQuery(
    string? Search,
    Guid? SupplierId,
    Guid? PurchaseId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20);
