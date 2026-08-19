namespace Application.DTOs.Dashboard;

/// <summary>One day's trading, as the counter thinks of it.</summary>
/// <param name="PurchaseTotal">
/// Null for anyone who may not see cost. What the shop spent buying stock is not a figure the
/// counter needs, and zero would read as "bought nothing today".
/// </param>
public record DashboardTodayDto(
    decimal SalesTotal,
    int InvoiceCount,
    decimal? PurchaseTotal,
    int PurchaseCount);

/// <summary>
/// The current month against the one before it. <paramref name="ChangePercent"/> is null when last
/// month had no sales — "up from nothing" is not a percentage.
/// </summary>
public record DashboardMonthDto(
    decimal SalesTotal,
    int InvoiceCount,
    /// <summary>Null for anyone who may not see cost — see <see cref="DashboardTodayDto"/>.</summary>
    decimal? PurchaseTotal,
    decimal LastMonthSalesTotal,
    decimal? ChangePercent);

/// <summary>
/// What is owed in both directions, with receivables split by how long they have been outstanding.
/// Money owed to the shop is the figure a parts counter loses sleep over, so it carries the ageing.
/// </summary>
/// <summary>
/// What the shop is owed and what it owes, as at a date.
/// <para>
/// The receivable comes from the party ledger, not from adding up invoices: a customer's balance
/// also carries advances he has paid and charges raised without a bill, neither of which any invoice
/// knows about. Walk-in credit is the one exception and is added separately — there is no party row
/// to hold it.
/// </para>
/// <para>
/// Ageing is measured from the due date, so a customer on 30-day terms is not reported as overdue
/// on the day he is billed. <see cref="ReceivableNotDue"/> is what makes the tile readable: without
/// it every rupee enters an overdue bucket the moment it is invoiced, and the number stops meaning
/// anything.
/// </para>
/// </summary>
public record MoneyPositionDto(
    decimal Receivable,
    int ReceivableInvoiceCount,
    int CustomersWithDues,
    decimal ReceivableNotDue,
    decimal ReceivableCurrent,
    decimal Receivable31To60,
    decimal ReceivableOver60,
    decimal Payable,
    int PayableBillCount,
    int SuppliersWithDues,
    /// <summary>
    /// Money held on account against no bill. Reported on its own rather than netted off the
    /// receivable, because one customer's advance is not another customer's payment.
    /// </summary>
    decimal AdvancesHeld);

/// <summary>One HSN code's contribution to the month, in the shape GSTR-1 Table 12 asks for.</summary>
public record HsnSummaryRowDto(
    string Hsn,
    string Uqc,
    decimal Quantity,
    decimal TaxableValue,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TotalTax);

/// <summary>
/// The month's GST position. Output is tax collected on sales, input is tax paid on purchases, and
/// the difference is what actually gets remitted.
/// </summary>
public record GstSummaryDto(
    decimal OutputTaxable,
    decimal OutputCgst,
    decimal OutputSgst,
    decimal OutputIgst,
    decimal OutputTotal,
    decimal InputTaxable,
    decimal InputCgst,
    decimal InputSgst,
    decimal InputIgst,
    decimal InputTotal,
    decimal NetPayable,
    IReadOnlyList<HsnSummaryRowDto> Hsn);

/// <summary>
/// The checks an auditor runs first. Everything here is a question about the documents themselves
/// rather than about trade, which is why it is separated from the trading figures.
/// </summary>
public record AuditChecksDto(
    string FinancialYear,
    IReadOnlyList<string> MissingInvoiceNumbers,
    int MissingInvoiceCount,
    IReadOnlyList<string> MissingPurchaseNumbers,
    int MissingPurchaseCount,
    /// <summary>
    /// Credit and debit notes run their own series, so they need their own gap check. Interleaving
    /// them with invoices would have put real holes in the invoice run while this tile stayed green.
    /// </summary>
    IReadOnlyList<string> MissingCreditNoteNumbers,
    int MissingCreditNoteCount,
    IReadOnlyList<string> MissingDebitNoteNumbers,
    int MissingDebitNoteCount,
    int CancelledInvoiceCount,
    int CancelledPurchaseCount,
    int StockAdjustmentCount,
    decimal StockAdjustmentNetQuantity,
    int B2BInvoiceCount,
    decimal B2BSales,
    int B2CInvoiceCount,
    decimal B2CSales,
    int HighValueWithoutGstinCount,
    decimal HighValueWithoutGstinThreshold,
    int ItemsSoldWithoutHsnCount,
    decimal SalesWithoutHsn,
    /// <summary>
    /// Rows whose denormalised total disagrees with the entries behind it. Every one of these must
    /// be zero; a non-zero count means a number on screen is no longer backed by anything, and the
    /// only way to find that out otherwise is for a customer to argue about his balance.
    /// </summary>
    ReconciliationChecksDto Reconciliation);

/// <summary>
/// The four counts that stand between "denormalised" and "wrong since some unknown Tuesday".
/// </summary>
public record ReconciliationChecksDto(
    int PartyBalanceMismatches,
    int DocumentBalanceMismatches,
    int AllocationMismatches,
    int StockMismatches)
{
    public int TotalMismatches =>
        PartyBalanceMismatches + DocumentBalanceMismatches + AllocationMismatches + StockMismatches;

    public bool IsClean => TotalMismatches == 0;
}

/// <summary>One bar on the trend chart. Days with no trade are present with zero, never missing.</summary>
public record SalesTrendPointDto(DateOnly Date, decimal SalesTotal, int InvoiceCount);

public record ReorderItemDto(
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string Uqc,
    decimal StockOnHand,
    decimal ReorderLevel);

public record TopSellingItemDto(
    Guid ProductId,
    string PartNumber,
    string ItemName,
    string Uqc,
    decimal Quantity,
    decimal SalesValue);

public record RecentInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string CustomerName,
    decimal GrandTotal,
    decimal BalanceDue,
    string Status);

/// <summary>
/// Everything the dashboard screen needs, in one response. Composed server-side rather than left to
/// the client so the screen has a single loading state instead of one spinner per panel.
/// </summary>
public record DashboardDto(
    DateOnly AsOf,
    DashboardTodayDto Today,
    DashboardMonthDto Month,
    MoneyPositionDto Money,
    /// <summary>
    /// Null for anyone who may not see the registers. The input side of this panel is what the shop
    /// spent on stock this month — the same figure the cost permission exists to keep off the
    /// counter, reached by a different route.
    /// </summary>
    GstSummaryDto? Gst,
    AuditChecksDto Audit,
    IReadOnlyList<SalesTrendPointDto> SalesTrend,
    IReadOnlyList<ReorderItemDto> Reorder,
    IReadOnlyList<TopSellingItemDto> TopSellers,
    IReadOnlyList<RecentInvoiceDto> RecentInvoices);
