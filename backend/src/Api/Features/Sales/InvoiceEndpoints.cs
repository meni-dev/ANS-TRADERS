using Application.DTOs.Invoices;
using Application.Interfaces;

namespace Api.Features.Sales;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices").WithTags("Invoices");

        group.MapGet("/", async (
            string? search,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
            Guid? customerId,
            bool? unpaidOnly,
            int page,
            int pageSize,
            IInvoiceService service,
            CancellationToken cancellationToken) =>
        {
            var query = new InvoiceListQuery(
                search, status, fromDate, toDate, customerId, unpaidOnly,
                page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.SearchAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id, IInvoiceService service, CancellationToken cancellationToken) =>
        {
            var invoice = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(invoice);
        });

        group.MapPost("/", async (
            CreateInvoiceRequest request, IInvoiceService service, CancellationToken cancellationToken) =>
        {
            var invoice = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/invoices/{invoice.Id}", invoice);
        });

        // No PUT: an issued invoice already went to a customer. Cancel and re-issue instead.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, IInvoiceService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });

        // Its own endpoint rather than part of InvoiceDto: the bill list would otherwise pay for
        // this aggregate on every row of every page.
        group.MapGet("/{id:guid}/returnable", async (
            Guid id, ICreditNoteService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetReturnableAsync(id, cancellationToken)));
    }
}
