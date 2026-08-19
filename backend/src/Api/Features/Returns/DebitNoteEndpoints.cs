using Application.DTOs.Returns;
using Application.Interfaces;

namespace Api.Features.Returns;

/// <summary>
/// Goods going back to a supplier. A debit note is a document in its own right, so this group
/// mirrors the purchase group — create, read, cancel, and no PUT.
/// </summary>
public static class DebitNoteEndpoints
{
    public static void MapDebitNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/debit-notes").WithTags("Debit Notes");

        group.MapGet("/", async (
            string? search,
            Guid? supplierId,
            Guid? purchaseId,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            IDebitNoteService service,
            CancellationToken cancellationToken) =>
        {
            var query = new DebitNoteListQuery(
                search, supplierId, purchaseId, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20);

            return Results.Ok(await service.SearchAsync(query, cancellationToken));
        });

        group.MapGet("/{id:guid}", async (
            Guid id, IDebitNoteService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetByIdAsync(id, cancellationToken)));

        group.MapPost("/", async (
            CreateDebitNoteRequest request,
            IDebitNoteService service,
            CancellationToken cancellationToken) =>
        {
            var note = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/debit-notes/{note.Id}", note);
        });

        // No PUT: a debit note handed to a customer is as immutable as the bill it debits.
        // Keyed wrong means cancel and re-enter, which leaves both rows on the statement.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, IDebitNoteService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });
    }
}
