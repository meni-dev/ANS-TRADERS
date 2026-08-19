using Application.DTOs.Customers;
using Application.Interfaces;

namespace Api.Features.Customers;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (
            string? search,
            bool? activeOnly,
            int page,
            int pageSize,
            ICustomerService service,
            CancellationToken cancellationToken) =>
        {
            var query = new CustomerListQuery(
                search, activeOnly, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.SearchAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ICustomerService service, CancellationToken cancellationToken) =>
        {
            var customer = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(customer);
        });

        group.MapPost("/", async (
            CreateCustomerRequest request, ICustomerService service, CancellationToken cancellationToken) =>
        {
            var customer = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/customers/{customer.Id}", customer);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateCustomerRequest request, ICustomerService service, CancellationToken cancellationToken) =>
        {
            var customer = await service.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(customer);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ICustomerService service, CancellationToken cancellationToken) =>
        {
            await service.DeactivateAsync(id, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (
            Guid id, ICustomerService service, CancellationToken cancellationToken) =>
        {
            await service.ActivateAsync(id, cancellationToken);
            return Results.NoContent();
        });

        // The party-side reads live here rather than under /api/payments: they are questions about
        // this customer, and the screen asking them is the customer's own.
        group.MapGet("/{id:guid}/ledger", async (
            Guid id,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            IPartyAccountService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetStatementAsync(
                id, null, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 50,
                cancellationToken)));

        group.MapGet("/{id:guid}/outstanding", async (
            Guid id, IPartyAccountService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetOpenDocumentsAsync(id, null, cancellationToken)));

        // Kept off CustomerDto deliberately — the customer list would otherwise pay for cheque and
        // ageing aggregates on every row of every page.
        group.MapGet("/{id:guid}/account-summary", async (
            Guid id, IPartyAccountService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetCustomerAccountSummaryAsync(id, cancellationToken)));
    }
}
