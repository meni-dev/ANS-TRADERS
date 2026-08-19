using Application.DTOs.Purchases;
using Application.Interfaces;

namespace Api.Features.Purchases;

public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/purchases").WithTags("Purchases");

        group.MapGet("/", async (
            string? search,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
            Guid? supplierId,
            int page,
            int pageSize,
            IPurchaseService service,
            CancellationToken cancellationToken) =>
        {
            var query = new PurchaseListQuery(
                search, status, fromDate, toDate, supplierId,
                page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.SearchAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, IPurchaseService service, CancellationToken cancellationToken) =>
        {
            var purchase = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(purchase);
        });

        group.MapPost("/", async (
            CreatePurchaseRequest request, IPurchaseService service, CancellationToken cancellationToken) =>
        {
            var purchase = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/purchases/{purchase.Id}", purchase);
        });

        // No PUT: a recorded purchase is a tax document. Cancel it and enter a fresh one instead.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, IPurchaseService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });

        // See the invoice group's twin: its own endpoint so the bill list does not pay for this
        // aggregate on every row.
        group.MapGet("/{id:guid}/returnable", async (
            Guid id, IDebitNoteService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetReturnableAsync(id, cancellationToken)));
    }
}
