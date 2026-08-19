using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Expenses;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly IDashboardRepository _trading;
    private readonly IPeriodLock _periodLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;

    private readonly IShopClock _clock;

    public ExpenseService(
        IExpenseRepository repository,
        IDashboardRepository trading,
        IPeriodLock periodLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IShopClock clock)
    {
        _repository = repository;
        _trading = trading;
        _periodLock = periodLock;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<PagedResult<ExpenseDto>> SearchAsync(
        ExpenseListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search, ParseCategory(query.Category), query.FromDate, query.ToDate,
            query.Page, query.PageSize, cancellationToken);

        return new PagedResult<ExpenseDto>(
            items.Select(ToDto).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<ExpenseDto> CreateAsync(
        CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ExpenseRecord, "record an expense");

        await _periodLock.GuardAsync(request.ExpenseDate, "expense", cancellationToken);

        var amount = Round(request.Amount);

        if (amount <= 0)
        {
            throw Invalid("Amount", "Enter what was spent");
        }

        if (request.ExpenseDate > _clock.Today)
        {
            throw Invalid("ExpenseDate", "An expense cannot be dated in the future");
        }

        if (!Enum.TryParse<ExpenseCategory>(request.Category, ignoreCase: true, out var category))
        {
            throw Invalid("Category", "Pick what the money went on");
        }

        // Credit is not a way of paying. On a bill it means nothing was tendered, and an expense
        // that tendered nothing did not happen.
        if (!Enum.TryParse<PaymentMode>(request.Mode, ignoreCase: true, out var mode)
            || mode == PaymentMode.Credit)
        {
            throw Invalid("Mode", "How was it paid?");
        }

        var financialYear = FinancialYear.For(request.ExpenseDate);
        var sequence = await _repository.GetLastSequenceAsync(financialYear, cancellationToken) + 1;

        var expense = new Expense
        {
            ExpenseNumber = $"EXP/{financialYear}/{sequence:D4}",
            FinancialYear = financialYear,
            Sequence = sequence,
            ExpenseDate = request.ExpenseDate,
            Category = category,
            Amount = amount,
            Mode = mode,
            ReferenceNumber = Clean(request.ReferenceNumber),
            PaidTo = Clean(request.PaidTo),
            Notes = Clean(request.Notes),
        };

        await _repository.AddAsync(expense, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return ToDto(expense);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Expense '{id}' was not found", "EXPENSE_NOT_FOUND");

        if (expense.IsCancelled)
        {
            throw new ConflictException(
                $"{expense.ExpenseNumber} is already cancelled", "EXPENSE_ALREADY_CANCELLED");
        }

        _currentUser.Require(Permission.ExpenseRecord, "cancel an expense");

        await _periodLock.GuardAsync(expense.ExpenseDate, "expense", cancellationToken);

        // Flagged, never deleted — the number stays taken so the series has no hole in it.
        expense.IsCancelled = true;
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(
            AuditAction.Cancelled,
            "Expense",
            expense.Id,
            expense.ExpenseNumber,
            $"{expense.Category} {expense.Amount:0.00}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExpenseSummaryDto> GetSummaryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var totals = await _repository.GetTotalsByCategoryAsync(fromDate, toDate, cancellationToken);

        return new ExpenseSummaryDto(
            Round(totals.Sum(t => t.Amount)),
            totals.Sum(t => t.Count),
            totals
                .OrderByDescending(t => t.Amount)
                .Select(t => new ExpenseCategoryTotalDto(
                    t.Category.ToString(), Label(t.Category), Round(t.Amount), t.Count))
                .ToList());
    }

    public async Task<ProfitAndLossDto> GetProfitAndLossAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.CostView, "see the shop's profit");

        var trading = await _trading.GetTradingResultAsync(fromDate, toDate, cancellationToken);
        var spend = await GetSummaryAsync(fromDate, toDate, cancellationToken);

        var gross = Round(trading.Revenue - trading.CostOfGoods);

        return new ProfitAndLossDto(
            fromDate,
            toDate,
            trading.Revenue,
            trading.CostOfGoods,
            gross,
            spend.Total,
            Round(gross - spend.Total),
            trading.CostedLines,
            trading.UncostedLines,
            spend.ByCategory);
    }

    /// <summary>
    /// What the category is called on screen. The enum name is for the database; a shopkeeper reads
    /// "Shop expenses", not "ShopExpenses".
    /// </summary>
    public static string Label(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Rent => "Rent",
        ExpenseCategory.Salary => "Salary and wages",
        ExpenseCategory.Utilities => "Electricity, water, phone",
        ExpenseCategory.Freight => "Freight and transport",
        ExpenseCategory.ShopExpenses => "Shop expenses",
        ExpenseCategory.BankCharges => "Bank charges and interest",
        ExpenseCategory.Marketing => "Advertising",
        ExpenseCategory.TaxesAndFees => "Taxes, licences and fees",
        ExpenseCategory.Repairs => "Repairs and maintenance",
        _ => "Other",
    };

    private static ExpenseDto ToDto(Expense e) => new(
        e.Id, e.ExpenseNumber, e.ExpenseDate, e.Category.ToString(), Label(e.Category),
        e.Amount, e.Mode.ToString(), e.ReferenceNumber, e.PaidTo, e.Notes, e.IsCancelled, e.CreatedAt);

    private static ExpenseCategory? ParseCategory(string? raw) =>
        Enum.TryParse<ExpenseCategory>(raw, ignoreCase: true, out var parsed) ? parsed : null;

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static ValidationAppException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
