using Application.Interfaces;

namespace Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (
                DateOnly? asOf,
                IDashboardService service,
                CancellationToken cancellationToken) =>
            {
                // The client sends its own date, so "today" is the shop's today. Falling back to the
                // server clock would put a counter in India on the wrong day for most of the evening.
                var date = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

                var dashboard = await service.GetAsync(date, cancellationToken);
                return Results.Ok(dashboard);
            })
            .WithTags("Dashboard");
    }
}
