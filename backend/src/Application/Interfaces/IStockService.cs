using Application.Common;
using Application.DTOs.Stock;

namespace Application.Interfaces;

public interface IStockService
{
    Task<PagedResult<ProductStockDto>> SearchAsync(StockListQuery query, CancellationToken cancellationToken);

    Task<StockSummaryDto> GetSummaryAsync(StockListQuery query, CancellationToken cancellationToken);

    Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        StockMovementListQuery query, CancellationToken cancellationToken);

    /// <summary>Corrects stock to a counted quantity and records why. See <c>AdjustStockRequest</c>.</summary>
    Task<ProductStockDto> AdjustAsync(AdjustStockRequest request, CancellationToken cancellationToken);

    /// <summary>What left the shelf without being sold, grouped by why.</summary>
    Task<StockLossReportDto> GetLossReportAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
