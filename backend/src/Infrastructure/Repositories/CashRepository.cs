using Application.DTOs.Cash;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CashRepository : ICashRepository
{
    private readonly AppDbContext _context;

    public CashRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<DayClose?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken) =>
        _context.DayCloses.FirstOrDefaultAsync(d => d.CloseDate == date, cancellationToken);

    public Task<DayClose?> GetLastCloseOnOrBeforeAsync(
        DateOnly date, CancellationToken cancellationToken) =>
        _context.DayCloses
            .AsNoTracking()
            .Where(d => d.CloseDate <= date)
            .OrderByDescending(d => d.CloseDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CashBookEntryDto>> GetCashMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        // Only Posted. A pending post-dated cheque has not moved anything, and cash is the one
        // figure somebody physically counts — it cannot include a promise.
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Mode == PaymentMode.Cash
                        && p.Status == PaymentStatus.Posted
                        && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .Select(p => new
            {
                p.PaymentDate,
                p.Direction,
                p.Amount,
                p.PartyName,
                p.ReceiptNumber,
                p.IsCounterPayment,
            })
            .ToListAsync(cancellationToken);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.Mode == PaymentMode.Cash
                        && !e.IsCancelled
                        && e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate)
            .Select(e => new { e.ExpenseDate, e.ExpenseNumber, e.Category, e.PaidTo, e.Amount })
            .ToListAsync(cancellationToken);

        var entries = new List<CashBookEntryDto>(payments.Count + expenses.Count);

        entries.AddRange(payments.Select(p => new CashBookEntryDto(
            p.PaymentDate,
            p.Direction == PaymentDirection.Received ? "Receipt" : "Paid out",
            // Counter money has no receipt of its own — the bill it was taken on is the reference.
            p.ReceiptNumber ?? (p.IsCounterPayment ? "On the bill" : string.Empty),
            p.PartyName,
            p.Direction == PaymentDirection.Received ? p.Amount : 0m,
            p.Direction == PaymentDirection.Paid ? p.Amount : 0m,
            0m)));

        entries.AddRange(expenses.Select(e => new CashBookEntryDto(
            e.ExpenseDate,
            "Expense",
            e.ExpenseNumber,
            string.IsNullOrWhiteSpace(e.PaidTo) ? e.Category.ToString() : $"{e.Category} — {e.PaidTo}",
            0m,
            e.Amount,
            0m)));

        return entries;
    }

    public async Task<IReadOnlyList<DayClose>> GetClosesBetweenAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.DayCloses
            .AsNoTracking()
            .Where(d => d.CloseDate >= fromDate && d.CloseDate <= toDate)
            .OrderBy(d => d.CloseDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DayClose close, CancellationToken cancellationToken) =>
        await _context.DayCloses.AddAsync(close, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
