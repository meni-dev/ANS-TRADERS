using Application.DTOs.Reports;
using Application.Interfaces;

namespace Api.Features.Reports;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/registers", (IReportService service) => Results.Ok(service.GetRegisters()));

        group.MapGet("/registers/{key}", async (
            string key,
            DateOnly fromDate,
            DateOnly toDate,
            IReportService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.BuildAsync(new RegisterQuery(key, fromDate, toDate), cancellationToken)));
    }
}
