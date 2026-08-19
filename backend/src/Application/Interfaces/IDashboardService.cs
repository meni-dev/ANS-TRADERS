using Application.DTOs.Dashboard;

namespace Application.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Composes the whole dashboard as at <paramref name="asOf"/>. The date comes from the client so
    /// "today" means the shop's today, not the server's.
    /// </summary>
    Task<DashboardDto> GetAsync(DateOnly asOf, CancellationToken cancellationToken);
}
