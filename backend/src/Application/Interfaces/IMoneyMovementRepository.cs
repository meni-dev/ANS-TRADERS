using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IMoneyMovementRepository
{
    Task<IReadOnlyList<MoneyMovement>> SearchAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<MoneyMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Live totals per kind, for the capital summary.</summary>
    Task<IReadOnlyDictionary<MoneyMovementKind, decimal>> GetTotalsAsync(CancellationToken cancellationToken);

    /// <summary>True when the shop has already said what it opened with — it can only do so once.</summary>
    Task<bool> ExistsAsync(MoneyMovementKind kind, CancellationToken cancellationToken);

    Task AddAsync(MoneyMovement movement, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
