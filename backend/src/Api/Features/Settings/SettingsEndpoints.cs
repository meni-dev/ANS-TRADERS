using Application.DTOs.Settings;
using Application.Interfaces;

namespace Api.Features.Settings;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/", async (IShopSettingsService service, CancellationToken cancellationToken) =>
        {
            var settings = await service.GetAsync(cancellationToken);
            return Results.Ok(settings);
        });

        // One row, so this is a PUT of the whole thing rather than a create/update pair.
        group.MapPut("/", async (
            UpdateShopSettingsRequest request,
            IShopSettingsService service,
            CancellationToken cancellationToken) =>
        {
            var settings = await service.UpdateAsync(request, cancellationToken);
            return Results.Ok(settings);
        });

        // Its own route because it is its own decision — owner only, and logged.
        group.MapPut("/books-lock", async (
            SetBooksLockRequest request,
            IShopSettingsService service,
            CancellationToken cancellationToken) =>
        {
            var settings = await service.SetBooksLockAsync(request, cancellationToken);
            return Results.Ok(settings);
        });
    }
}
