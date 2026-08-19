using Application.DTOs.Expenses;
using Application.Interfaces;

namespace Api.Features.Expenses;

/// <summary>
/// What the shop spends on running itself. No PUT — a recorded expense is corrected by cancelling
/// and re-entering, so both rows stay on the books.
/// </summary>
public static class ExpenseEndpoints
{
    public static void MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses").WithTags("Expenses");

        group.MapGet("/", async (
            string? search,
            string? category,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            IExpenseService service,
            CancellationToken cancellationToken) =>
        {
            var query = new ExpenseListQuery(
                search, category, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20);

            return Results.Ok(await service.SearchAsync(query, cancellationToken));
        });

        group.MapGet("/summary", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            IExpenseService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var to = toDate ?? today;
            var from = fromDate ?? new DateOnly(to.Year, to.Month, 1);

            return Results.Ok(await service.GetSummaryAsync(from, to, cancellationToken));
        });

        // Its own route rather than part of the dashboard: a P&L is asked for over a chosen range,
        // while the dashboard answers for today and this month.
        group.MapGet("/profit-and-loss", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            IExpenseService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var to = toDate ?? today;
            var from = fromDate ?? new DateOnly(to.Year, to.Month, 1);

            return Results.Ok(await service.GetProfitAndLossAsync(from, to, cancellationToken));
        });

        group.MapPost("/", async (
            CreateExpenseRequest request, IExpenseService service, CancellationToken cancellationToken) =>
        {
            var expense = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/expenses/{expense.Id}", expense);
        });

        group.MapPost("/{id:guid}/cancel", async (
            Guid id, IExpenseService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });
    }
}
