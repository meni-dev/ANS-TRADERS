using Application.DTOs.Stock;

namespace Application.Interfaces;

public interface IStockInsightService
{
    Task<DeadStockReportDto> GetDeadStockAsync(int? monthsWithoutSale, CancellationToken cancellationToken);

    Task<RateDriftReportDto> GetRateDriftAsync(decimal? marginFloorPercent, CancellationToken cancellationToken);

    Task<ReorderReportDto> GetReorderAsync(int? coverDays, CancellationToken cancellationToken);
}
