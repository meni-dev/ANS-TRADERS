namespace Application.DTOs.Expenses;

public record CreateExpenseRequest(
    DateOnly ExpenseDate,
    string Category,
    decimal Amount,
    string Mode,
    string? ReferenceNumber,
    string? PaidTo,
    string? Notes);

public record ExpenseDto(
    Guid Id,
    string ExpenseNumber,
    DateOnly ExpenseDate,
    string Category,
    string CategoryLabel,
    decimal Amount,
    string Mode,
    string? ReferenceNumber,
    string? PaidTo,
    string? Notes,
    bool IsCancelled,
    DateTimeOffset CreatedAt);

public record ExpenseListQuery(
    string? Search,
    string? Category,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 20);

public record ExpenseCategoryTotalDto(string Category, string CategoryLabel, decimal Amount, int Count);

/// <summary>Spend over a range, and where it went — the second half of the profit question.</summary>
public record ExpenseSummaryDto(
    decimal Total,
    int Count,
    IReadOnlyList<ExpenseCategoryTotalDto> ByCategory);

/// <summary>
/// What the shop actually earned over a range.
/// <para>
/// <see cref="CostedLines"/> and <see cref="UncostedLines"/> are not decoration. Cost is snapshotted
/// on a sale line from the day that was built; every line sold before then has no cost and is left
/// out of <see cref="CostOfGoods"/>. A gross profit computed over a period that is mostly uncosted
/// lines is close to meaningless, and the figure has to say so rather than look confident.
/// </para>
/// </summary>
public record ProfitAndLossDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal Revenue,
    decimal CostOfGoods,
    decimal GrossProfit,
    decimal Expenses,
    decimal NetProfit,
    int CostedLines,
    int UncostedLines,
    IReadOnlyList<ExpenseCategoryTotalDto> ExpensesByCategory)
{
    /// <summary>How much of the period's sale lines carried a known cost. 100 means the figure is whole.</summary>
    public decimal CostCoveragePercent =>
        CostedLines + UncostedLines == 0
            ? 100m
            : Math.Round(100m * CostedLines / (CostedLines + UncostedLines), 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// True when every line in the period had a cost. When false the caller must show the coverage
    /// beside the figure rather than presenting it as the shop's profit.
    /// </summary>
    public bool IsComplete => UncostedLines == 0;
}
