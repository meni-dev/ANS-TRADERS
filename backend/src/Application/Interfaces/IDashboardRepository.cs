using Application.DTOs.Dashboard;

namespace Application.Interfaces;

/// <summary>
/// Read-only aggregates for the dashboard. Unlike the other repositories this one returns DTOs
/// rather than entities: every method is a <c>GROUP BY</c> with no row behind it to hand back, and
/// threading twenty summed columns through tuples would obscure more than it protects.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Sales and purchases on a single day.</summary>
    Task<DashboardTodayDto> GetDayTotalsAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>Sales, purchases and invoice count over an inclusive date range.</summary>
    Task<(decimal SalesTotal, int InvoiceCount, decimal PurchaseTotal)> GetRangeTotalsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Outstanding in both directions, receivables aged against <paramref name="asOf"/>.</summary>
    Task<MoneyPositionDto> GetMoneyPositionAsync(DateOnly asOf, CancellationToken cancellationToken);

    Task<GstSummaryDto> GetGstSummaryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>
    /// Document-integrity checks for the financial year containing <paramref name="asOf"/>, plus
    /// month-scoped counts of the things that need explaining.
    /// </summary>
    Task<AuditChecksDto> GetAuditChecksAsync(
        DateOnly asOf,
        DateOnly monthStart,
        DateOnly monthEnd,
        string financialYear,
        decimal highValueThreshold,
        CancellationToken cancellationToken);

    /// <summary>
    /// Daily sales between the two dates. Days with no trade are absent from the result — the
    /// service fills them, so the chart never has to know about gaps.
    /// </summary>
    Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<TopSellingItemDto>> GetTopSellingAsync(
        DateOnly fromDate, DateOnly toDate, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Revenue, cost of goods and the coverage behind that cost, over a range. Expenses are added
    /// by the service — they live in their own repository.
    /// </summary>
    Task<(decimal Revenue, decimal CostOfGoods, int CostedLines, int UncostedLines)> GetTradingResultAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecentInvoiceDto>> GetRecentInvoicesAsync(
        int limit, CancellationToken cancellationToken);
}
