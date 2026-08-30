using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;

    public ExpenseRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Expense> Items, int TotalCount)> SearchAsync(
        string? search,
        ExpenseCategory? category,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Expenses.AsNoTracking().AsQueryable();

        if (category is { } c) query = query.Where(e => e.Category == c);
        if (fromDate is { } from) query = query.Where(e => e.ExpenseDate >= from);
        if (toDate is { } to) query = query.Where(e => e.ExpenseDate <= to);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.ExpenseNumber, pattern) ||
                (e.PaidTo != null && EF.Functions.ILike(e.PaidTo, pattern)) ||
                (e.Notes != null && EF.Functions.ILike(e.Notes, pattern)) ||
                (e.ReferenceNumber != null && EF.Functions.ILike(e.ReferenceNumber, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first, like every other document list.
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<(ExpenseCategory Category, decimal Amount, int Count)>>
        GetTotalsByCategoryAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var rows = await _context.Expenses
            .AsNoTracking()
            .Where(e => !e.IsCancelled && e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(e => e.Amount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Category, r.Amount, r.Count)).ToList();
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken) =>
        await _context.Expenses.AddAsync(expense, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
