using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Expense> Items, int TotalCount)> SearchAsync(
        string? search,
        ExpenseCategory? category,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetLastSequenceAsync(string financialYear, CancellationToken cancellationToken);

    /// <summary>Live spend over a range, grouped by where it went. Cancelled rows are excluded.</summary>
    Task<IReadOnlyList<(ExpenseCategory Category, decimal Amount, int Count)>> GetTotalsByCategoryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task AddAsync(Expense expense, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
