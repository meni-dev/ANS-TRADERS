using Application.DTOs.Cash;

namespace Application.Interfaces;

/// <summary>
/// The drawer. What should be in it, what was in it, and every movement between.
/// </summary>
public interface ICashService
{
    Task<CashPositionDto> GetPositionAsync(DateOnly date, CancellationToken cancellationToken);

    Task<DayCloseDto> CloseDayAsync(CloseDayRequest request, CancellationToken cancellationToken);

    Task<CashBookDto> GetCashBookAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
