using Application.DTOs.Suppliers;
using Application.Interfaces;

namespace Api.Features.Suppliers;

public static class SupplierEndpoints
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/suppliers").WithTags("Suppliers");

        group.MapGet("/", async (
            string? search,
            bool? activeOnly,
            int page,
            int pageSize,
            ISupplierService service,
            CancellationToken cancellationToken) =>
        {
            var query = new SupplierListQuery(
                search, activeOnly, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.SearchAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ISupplierService service, CancellationToken cancellationToken) =>
        {
            var supplier = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(supplier);
        });

        group.MapPost("/", async (
            CreateSupplierRequest request, ISupplierService service, CancellationToken cancellationToken) =>
        {
            var supplier = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/suppliers/{supplier.Id}", supplier);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateSupplierRequest request, ISupplierService service, CancellationToken cancellationToken) =>
        {
            var supplier = await service.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(supplier);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISupplierService service, CancellationToken cancellationToken) =>
        {
            await service.DeactivateAsync(id, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (
            Guid id, ISupplierService service, CancellationToken cancellationToken) =>
        {
            await service.ActivateAsync(id, cancellationToken);
            return Results.NoContent();
        });

        // Mirrors the customer side. No account summary: the credit warning exists to protect money
        // the shop is owed, and nobody extends the shop a limit.
        group.MapGet("/{id:guid}/ledger", async (
            Guid id,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            IPartyAccountService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetStatementAsync(
                null, id, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 50,
                cancellationToken)));

        group.MapGet("/{id:guid}/outstanding", async (
            Guid id, IPartyAccountService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetOpenDocumentsAsync(null, id, cancellationToken)));
    }
}
