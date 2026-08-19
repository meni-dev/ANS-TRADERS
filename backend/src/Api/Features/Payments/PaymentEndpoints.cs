using Application.DTOs.Payments;
using Application.Interfaces;

namespace Api.Features.Payments;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapGet("/", async (
            string? search,
            string? direction,
            string? status,
            string? mode,
            Guid? customerId,
            Guid? supplierId,
            DateOnly? fromDate,
            DateOnly? toDate,
            bool? unallocatedOnly,
            int? page,
            int? pageSize,
            IPaymentService service,
            CancellationToken cancellationToken) =>
        {
            var query = new PaymentListQuery(
                search, direction, status, mode, customerId, supplierId, fromDate, toDate,
                unallocatedOnly, page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20);

            return Results.Ok(await service.SearchAsync(query, cancellationToken));
        });

        // Money in against money out for a range. Cheques still settling are reported separately —
        // see PaymentSummaryDto for why they must not be added to the collected figure.
        group.MapGet("/summary", async (
            DateOnly? fromDate,
            DateOnly? toDate,
            IPaymentService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
        {
            var today = clock.Today;
            var to = toDate ?? today;
            var from = fromDate ?? to;

            return Results.Ok(await service.GetSummaryAsync(from, to, cancellationToken));
        });

        group.MapGet("/dues", async (IPaymentService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetDuesAsync(cancellationToken)));

        group.MapGet("/{id:guid}", async (
            Guid id, IPaymentService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetByIdAsync(id, cancellationToken)));

        group.MapPost("/", async (
            CreatePaymentRequest request, IPaymentService service, CancellationToken cancellationToken) =>
        {
            var payment = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/payments/{payment.Id}", payment);
        });

        // Spends money already sitting on account. Separate from creation because an advance is
        // taken on one day and used on another.
        group.MapPost("/{id:guid}/allocate", async (
            Guid id,
            AllocatePaymentRequest request,
            IPaymentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.AllocateAsync(id, request, cancellationToken)));

        // No PUT anywhere in this group: a receipt handed to a customer is as immutable as an
        // invoice. Keyed wrong means cancel and re-enter, which leaves both rows on the statement.
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, IPaymentService service, CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(id, cancellationToken);
            return Results.NoContent();
        });

        // The only way to move a party balance with no document behind it — a write-off, a rounding
        // difference settled by hand. Mirrors POST /api/stock/adjust.
        group.MapPost("/adjust", async (
            AdjustPartyBalanceRequest request,
            IPaymentService service,
            CancellationToken cancellationToken) =>
        {
            await service.AdjustAsync(request, cancellationToken);
            return Results.NoContent();
        });
    }
}
