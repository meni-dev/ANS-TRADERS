using Application.DTOs.Cash;

namespace Application.Interfaces;

/// <summary>
/// The drawer. What should be in it, what was in it, and every movement between.
/// </summary>
public interface ICashService
{
    Task<CashPositionDto> GetPositionAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// What the drawer should hold on <paramref name="date"/>, with no permission check.
    /// <para>
    /// For services that need the figure to keep an invariant rather than to show it. Somebody
    /// allowed to move the shop's capital is not necessarily allowed to see the drawer, and
    /// refusing them on a check they never asked for would report a permission problem for what is
    /// really an arithmetic one.
    /// </para>
    /// </summary>
    Task<decimal> GetExpectedCashAsync(DateOnly date, CancellationToken cancellationToken);

    Task<DayCloseDto> CloseDayAsync(CloseDayRequest request, CancellationToken cancellationToken);

    Task<CashBookDto> GetCashBookAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
