using Application.DTOs.Cash;

namespace Application.Interfaces;

public interface IMoneyMovementService
{
    Task<IReadOnlyList<MoneyMovementDto>> SearchAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<MoneyMovementDto> RecordAsync(
        RecordMoneyMovementRequest request, CancellationToken cancellationToken);

    Task CancelAsync(Guid id, CancellationToken cancellationToken);

    Task<CapitalSummaryDto> GetCapitalAsync(CancellationToken cancellationToken);
}
