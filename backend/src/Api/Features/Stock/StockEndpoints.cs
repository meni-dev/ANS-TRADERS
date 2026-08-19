using Application.DTOs.Stock;
using Application.Interfaces;

namespace Api.Features.Stock;

public static class StockEndpoints
{
    public static void MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock").WithTags("Stock");

        group.MapGet("/", async (
            string? search,
            bool? lowOnly,
            bool? activeOnly,
            int page,
            int pageSize,
            IStockService service,
            CancellationToken cancellationToken) =>
        {
            var query = new StockListQuery(
                search, lowOnly, activeOnly, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.SearchAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/summary", async (
            string? search,
            bool? lowOnly,
            bool? activeOnly,
            IStockService service,
            CancellationToken cancellationToken) =>
        {
            var summary = await service.GetSummaryAsync(
                new StockListQuery(search, lowOnly, activeOnly), cancellationToken);

            return Results.Ok(summary);
        });

        group.MapGet("/movements", async (
            string? search,
            Guid? productId,
            string? movementType,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page,
            int pageSize,
            IStockService service,
            CancellationToken cancellationToken) =>
        {
            var query = new StockMovementListQuery(
                search, productId, movementType, fromDate, toDate,
                page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.GetMovementsAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        // The only way to change stock without a document behind it. Everything else moves through
        // a purchase or an invoice.
        group.MapPost("/adjust", async (
            AdjustStockRequest request, IStockService service, CancellationToken cancellationToken) =>
        {
            var stock = await service.AdjustAsync(request, cancellationToken);
            return Results.Ok(stock);
        });

        // "How much did I lose to damage this year" — a question a free-text reason could never
        // answer, which is why adjustments now carry a code.
        group.MapGet("/losses", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            IStockService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var to = toDate ?? today;

            // Defaults to the financial year containing the end date — losses are a year-scale
            // question, not a monthly one.
            var yearStart = new DateOnly(to.Month >= 4 ? to.Year : to.Year - 1, 4, 1);
            var from = fromDate ?? yearStart;

            return Results.Ok(await service.GetLossReportAsync(from, to, cancellationToken));
        });

        // The three shelf questions a parts shop cannot answer from a stock list: what is not
        // moving, what is no longer worth what it costs, and what is about to run out.
        group.MapGet("/dead-stock", async (
            int? months, IStockInsightService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetDeadStockAsync(months, cancellationToken)));

        group.MapGet("/rate-drift", async (
            decimal? marginFloor, IStockInsightService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetRateDriftAsync(marginFloor, cancellationToken)));

        group.MapGet("/reorder", async (
            int? coverDays, IStockInsightService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetReorderAsync(coverDays, cancellationToken)));
    }
}
