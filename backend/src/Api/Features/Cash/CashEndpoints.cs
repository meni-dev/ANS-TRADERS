using Application.DTOs.Cash;
using Application.Interfaces;

namespace Api.Features.Cash;

/// <summary>The drawer — what should be in it, and what was.</summary>
public static class CashEndpoints
{
    public static void MapCashEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cash").WithTags("Cash");

        group.MapGet("/position", async (
            DateOnly? date, ICashService service, IShopClock clock, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPositionAsync(
                date ?? clock.Today, cancellationToken)));

        group.MapGet("/book", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            ICashService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var to = toDate ?? today;
            var from = fromDate ?? new DateOnly(to.Year, to.Month, 1);

            return Results.Ok(await service.GetCashBookAsync(from, to, cancellationToken));
        });

        // A close is a statement about a moment, so it is written once and never edited. A wrong
        // count is corrected by an adjustment on the next day, not by rewriting yesterday.
        group.MapPost("/close", async (
            CloseDayRequest request, ICashService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CloseDayAsync(request, cancellationToken)));

        // Money with no party behind it. Its own group under cash because that is where somebody
        // looking for "where did the till get this from" will go.
        var money = app.MapGroup("/api/money").WithTags("Money");

        money.MapGet("/", async (
            DateOnly? fromDate, DateOnly? toDate,
            IMoneyMovementService service, IShopClock clock, CancellationToken cancellationToken) =>
        {
            var to = toDate ?? clock.Today;
            var from = fromDate ?? new DateOnly(to.Year, to.Month, 1);

            return Results.Ok(await service.SearchAsync(from, to, cancellationToken));
        });

        money.MapPost("/", async (
            RecordMoneyMovementRequest request,
            IMoneyMovementService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.RecordAsync(request, cancellationToken)));

        money.MapPost("/{id:guid}/cancel", async (
            Guid id, IMoneyMovementService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });

        money.MapGet("/capital", async (IMoneyMovementService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetCapitalAsync(cancellationToken)));
    }
}
