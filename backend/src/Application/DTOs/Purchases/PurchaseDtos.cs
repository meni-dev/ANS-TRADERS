namespace Application.DTOs.Purchases;

/// <summary>What the client sends for one line. Every money figure is derived server-side.</summary>
public record CreatePurchaseItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal Rate,
    decimal DiscountPercent);

public record CreatePurchaseRequest(
    Guid SupplierId,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    string PaymentMode,
    decimal AmountPaid,
    string? Notes,
    IReadOnlyList<CreatePurchaseItemRequest> Items);

public record PurchaseItemDto(
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

/// <summary>Full document, returned by the detail endpoint and after a create.</summary>
public record PurchaseDto(
    Guid Id,
    string PurchaseNumber,
    string FinancialYear,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    Guid SupplierId,
    string SupplierName,
    string? SupplierGstin,
    string? SupplierStateCode,
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
    IReadOnlyList<PurchaseItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Row shape for the list screen. Lines are left out on purpose — the grid never shows them, and
/// loading them for every row turns one query into a fan-out.
/// </summary>
public record PurchaseListItemDto(
    Guid Id,
    string PurchaseNumber,
    string SupplierInvoiceNumber,
    DateOnly InvoiceDate,
    Guid SupplierId,
    string SupplierName,
    int ItemCount,
    decimal TaxableAmount,
    decimal TotalTax,
    decimal GrandTotal,
    decimal AmountPaid,
    decimal BalanceDue,
    string PaymentMode,
    string Status);

public record PurchaseListQuery(
    string? Search,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? SupplierId,
    int Page = 1,
    int PageSize = 20);
