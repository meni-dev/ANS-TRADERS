using Application.DTOs.Returns;
using Application.Interfaces;

namespace Api.Features.Returns;

/// <summary>
/// Goods coming back from a customer. A credit note is a document in its own right, so this group
/// mirrors the invoice group — create, read, cancel, and no PUT.
/// </summary>
public static class CreditNoteEndpoints
{
    public static void MapCreditNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/credit-notes").WithTags("Credit Notes");

        group.MapGet("/", async (
            string? search,
            Guid? customerId,
            Guid? invoiceId,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            ICreditNoteService service,
            CancellationToken cancellationToken) =>
        {
            var query = new CreditNoteListQuery(
                search, customerId, invoiceId, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20);

            return Results.Ok(await service.SearchAsync(query, cancellationToken));
        });

        group.MapGet("/{id:guid}", async (
            Guid id, ICreditNoteService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetByIdAsync(id, cancellationToken)));

        group.MapPost("/", async (
            CreateCreditNoteRequest request,
            ICreditNoteService service,
            CancellationToken cancellationToken) =>
        {
            var note = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/credit-notes/{note.Id}", note);
        });

        // No PUT: a credit note handed to a customer is as immutable as the invoice it credits.
        // Keyed wrong means cancel and re-enter, which leaves both rows on the statement.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, ICreditNoteService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });
    }
}
