using Application.Common;
using Application.DTOs.Expenses;

namespace Application.Interfaces;

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> SearchAsync(ExpenseListQuery query, CancellationToken cancellationToken);

    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>Keyed wrong. The row and its number survive, like every other document here.</summary>
    Task CancelAsync(Guid id, CancellationToken cancellationToken);

    Task<ExpenseSummaryDto> GetSummaryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>
    /// What the shop earned over a range: revenue, less what the goods cost, less what it cost to
    /// keep the doors open. Carries its own cost coverage — see <see cref="ProfitAndLossDto"/>.
    /// </summary>
    Task<ProfitAndLossDto> GetProfitAndLossAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
