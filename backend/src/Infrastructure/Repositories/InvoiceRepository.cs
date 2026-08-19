using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Invoice> Items, int TotalCount)> SearchAsync(
        string? search,
        InvoiceStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? customerId,
        bool? unpaidOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // No Include of Items: the list projects ItemCount off the document itself.
        var query = _context.Invoices.AsNoTracking();

        if (status is { } statusFilter)
        {
            query = query.Where(i => i.Status == statusFilter);
        }

        if (customerId is { } customer)
        {
            query = query.Where(i => i.CustomerId == customer);
        }

        if (fromDate is { } from)
        {
            query = query.Where(i => i.InvoiceDate >= from);
        }

        if (toDate is { } to)
        {
            query = query.Where(i => i.InvoiceDate <= to);
        }

        if (unpaidOnly == true)
        {
            // A cancelled invoice keeps its balance figure but is not money anybody owes.
            query = query.Where(i => i.BalanceDue > 0 && i.Status != InvoiceStatus.Cancelled);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.InvoiceNumber, pattern) ||
                EF.Functions.ILike(i.CustomerName, pattern) ||
                (i.CustomerPhone != null && EF.Functions.ILike(i.CustomerPhone, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first: the counter almost always wants the bill just raised.
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> GetLastSequenceAsync(string financialYear, CancellationToken cancellationToken) =>
        await _context.Invoices
            .Where(i => i.FinancialYear == financialYear)
            .Select(i => (int?)i.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken) =>
        await _context.Invoices.AddAsync(invoice, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
