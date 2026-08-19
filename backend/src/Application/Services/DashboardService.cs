using Application.Common;
using Application.DTOs.Dashboard;
using Application.Interfaces;
using Application.Mapping;
using Domain.Enums;

namespace Application.Services;

public class DashboardService : IDashboardService
{
    /// <summary>Bars on the trend chart. A month of history reads at a glance; a quarter does not.</summary>
    private const int TrendDays = 30;

    private const int ListSize = 6;
    private const int RecentInvoiceCount = 5;

    /// <summary>
    /// A sale this size to someone with no GSTIN is worth a second look — the buyer is almost
    /// certainly a business that could have claimed input credit, and did not.
    /// </summary>
    private const decimal HighValueWithoutGstinThreshold = 50_000m;

    private readonly IDashboardRepository _repository;
    private readonly IStockRepository _stockRepository;
    private readonly ICurrentUser _currentUser;

    public DashboardService(
        IDashboardRepository repository,
        IStockRepository stockRepository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _stockRepository = stockRepository;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> GetAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(asOf.Year, asOf.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var lastMonthStart = monthStart.AddMonths(-1);
        var lastMonthEnd = monthStart.AddDays(-1);
        var trendStart = asOf.AddDays(-(TrendDays - 1));

        var showCost = _currentUser.Has(Permission.CostView);

        var today = await _repository.GetDayTotalsAsync(asOf, cancellationToken);

        if (!showCost)
        {
            today = today with { PurchaseTotal = null };
        }

        var month = await _repository.GetRangeTotalsAsync(monthStart, monthEnd, cancellationToken);
        var lastMonth = await _repository.GetRangeTotalsAsync(
            lastMonthStart, lastMonthEnd, cancellationToken);

        var money = await _repository.GetMoneyPositionAsync(asOf, cancellationToken);
        var gst = _currentUser.Has(Permission.ReportView)
            ? await _repository.GetGstSummaryAsync(monthStart, monthEnd, cancellationToken)
            : null;

        var audit = await _repository.GetAuditChecksAsync(
            asOf,
            monthStart,
            monthEnd,
            FinancialYear.For(asOf),
            HighValueWithoutGstinThreshold,
            cancellationToken);

        var trend = await _repository.GetSalesTrendAsync(trendStart, asOf, cancellationToken);
        var topSellers = await _repository.GetTopSellingAsync(
            monthStart, monthEnd, ListSize, cancellationToken);
        var recent = await _repository.GetRecentInvoicesAsync(RecentInvoiceCount, cancellationToken);

        // Reuses the stock screen's own query rather than a second low-stock rule. The decision that
        // a discontinued part is never on the reorder list already lives there.
        var (reorderItems, _) = await _stockRepository.SearchStockAsync(
            search: null, lowOnly: true, activeOnly: true, page: 1, pageSize: ListSize, cancellationToken);

        return new DashboardDto(
            asOf,
            today,
            new DashboardMonthDto(
                month.SalesTotal,
                month.InvoiceCount,
                showCost ? month.PurchaseTotal : null,
                lastMonth.SalesTotal,
                PercentChange(lastMonth.SalesTotal, month.SalesTotal)),
            money,
            gst,
            audit,
            FillMissingDays(trend, trendStart, asOf),
            reorderItems.Select(p => p.ToReorderDto()).ToList(),
            topSellers,
            recent);
    }

    /// <summary>
    /// Null rather than zero when the previous month had no sales: a jump from nothing is not a
    /// percentage, and showing "+100%" for the first month of trading would be nonsense.
    /// </summary>
    private static decimal? PercentChange(decimal previous, decimal current)
    {
        if (previous == 0)
        {
            return null;
        }

        return Math.Round((current - previous) / previous * 100m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// A <c>GROUP BY</c> returns nothing for a day with no sales, which would make the chart compress
    /// quiet days out of existence and misrepresent the shape of the month. Every day in the window
    /// gets a bar, even if it is zero.
    /// </summary>
    private static IReadOnlyList<SalesTrendPointDto> FillMissingDays(
        IReadOnlyList<SalesTrendPointDto> points, DateOnly from, DateOnly to)
    {
        var byDate = points.ToDictionary(p => p.Date);
        var filled = new List<SalesTrendPointDto>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            filled.Add(byDate.TryGetValue(date, out var point)
                ? point
                : new SalesTrendPointDto(date, 0m, 0));
        }

        return filled;
    }
}
