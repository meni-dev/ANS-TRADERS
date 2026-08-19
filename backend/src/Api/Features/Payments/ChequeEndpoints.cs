using Application.DTOs.Payments;
using Application.Interfaces;

namespace Api.Features.Payments;

/// <summary>
/// The cheque register. Every route here moves one cheque one step; the legal steps are declared in
/// <c>ChequeTransitions</c> and an illegal one comes back 409 rather than being quietly ignored.
/// </summary>
public static class ChequeEndpoints
{
    public static void MapChequeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cheques").WithTags("Cheques");

        group.MapGet("/", async (
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            IChequeService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
        {
            var query = new ChequeListQuery(
                status, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 20);

            return Results.Ok(await service.SearchAsync(query, cancellationToken));
        });

        group.MapPost("/{paymentId:guid}/deposit", async (
            Guid paymentId,
            ChequeStatusRequest? request,
            IChequeService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.DepositAsync(paymentId, OnDate(request, clock), cancellationToken)));

        group.MapPost("/{paymentId:guid}/clear", async (
            Guid paymentId,
            ChequeStatusRequest? request,
            IChequeService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ClearAsync(paymentId, OnDate(request, clock), cancellationToken)));

        // A post-dated cheque walked to the bank. This is what replaces a scheduler: the shop has to
        // make the trip anyway, so the human action already exists.
        group.MapPost("/{paymentId:guid}/post", async (
            Guid paymentId,
            ChequeStatusRequest? request,
            IChequeService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.PostAsync(paymentId, OnDate(request, clock), cancellationToken)));

        // Not a cancel. The money genuinely arrived and then failed, and the shop needs to be able
        // to see that it did before taking this customer's cheque again.
        group.MapPost("/{paymentId:guid}/bounce", async (
            Guid paymentId,
            BounceChequeRequest request,
            IChequeService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.BounceAsync(paymentId, request, cancellationToken)));

        group.MapPost("/{paymentId:guid}/cancel", async (
            Guid paymentId,
            ChequeStatusRequest? request,
            IChequeService service,
            IShopClock clock,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.CancelAsync(paymentId, OnDate(request, clock), cancellationToken)));
    }

    /// <summary>
    /// The register's row actions carry no body — the shop is recording what happened just now. A
    /// date is still accepted, because catching up on Monday for a Friday deposit is normal.
    /// </summary>
    private static DateOnly OnDate(ChequeStatusRequest? request, IShopClock clock) =>
        request?.OnDate ?? clock.Today;
}
