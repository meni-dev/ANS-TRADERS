namespace Application.DTOs.Invoices;

/// <summary>What the client sends for one line. Every money figure is derived server-side.</summary>
public record CreateInvoiceItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal Rate,
    decimal DiscountPercent);

/// <summary>
/// <paramref name="CustomerId"/> is null for a walk-in, in which case <paramref name="WalkInName"/>
/// carries who was billed.
/// </summary>
public record CreateInvoiceRequest(
    Guid? CustomerId,
    string? WalkInName,
    DateOnly InvoiceDate,
    string PaymentMode,
    decimal AmountPaid,
    string? Notes,
    /// <summary>Percentage off the whole bill. Ignored when <see cref="BillDiscountAmount"/> is given.</summary>
    decimal BillDiscountPercent,
    /// <summary>
    /// A flat amount off the whole bill — how the counter actually thinks ("make it ₹950"). Wins
    /// over the percentage when both are sent, because it is the more specific instruction.
    /// </summary>
    decimal BillDiscountAmount,
    IReadOnlyList<CreateInvoiceItemRequest> Items,
    /// <summary>
    /// How a part payment on a credit bill actually arrived. "Credit" is not a tender, so without
    /// this there is nowhere to record that the ₹300 was cash. Ignored on a non-credit bill, where
    /// the payment mode already says it. Defaults to cash.
    /// </summary>
    string? TenderMode = null,
    /// <summary>UPI reference or card slip number for the tender, when there is one.</summary>
    string? TenderReference = null,
    /// <summary>Cheque particulars, when the counter took a cheque.</summary>
    Payments.ChequeRequest? Cheque = null);

public record InvoiceItemDto(
    Guid Id,
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

/// <summary>Full document, returned by the detail endpoint and after a create. Drives the printed bill.</summary>
public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string FinancialYear,
    DateOnly InvoiceDate,
    /// <summary>
    /// When payment is expected — the invoice date plus the customer's credit days. Ageing is
    /// measured from here, so a customer on terms is not reported late the day he is billed.
    /// </summary>
    DateOnly? DueDate,
    Guid? CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerGstin,
    string? CustomerStateCode,
    bool IsInterState,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TotalTax,
    decimal RoundOff,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    string PaymentMode,
    string? Notes,
    string Status,
    IReadOnlyList<InvoiceItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Row shape for the list screen. See the note on the purchase equivalent.</summary>
public record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    Guid? CustomerId,
    string CustomerName,
    string? CustomerPhone,
    int ItemCount,
    decimal TaxableAmount,
    decimal TotalTax,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    string PaymentMode,
    string Status);

public record InvoiceListQuery(
    string? Search,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? CustomerId,
    bool? UnpaidOnly,
    int Page = 1,
    int PageSize = 20);
