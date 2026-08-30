using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PurchaseRepository : IPurchaseRepository
{
    private readonly AppDbContext _context;

    public PurchaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Purchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Purchase> Items, int TotalCount)> SearchAsync(
        string? search,
        PurchaseStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? supplierId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // No Include of Items: the list projects ItemCount off the document itself.
        var query = _context.Purchases.AsNoTracking();

        if (status is { } statusFilter)
        {
            query = query.Where(p => p.Status == statusFilter);
        }

        if (supplierId is { } supplier)
        {
            query = query.Where(p => p.SupplierId == supplier);
        }

        if (fromDate is { } from)
        {
            query = query.Where(p => p.InvoiceDate >= from);
        }

        if (toDate is { } to)
        {
            query = query.Where(p => p.InvoiceDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.PurchaseNumber, pattern) ||
                EF.Functions.ILike(p.SupplierInvoiceNumber, pattern) ||
                EF.Functions.ILike(p.SupplierName, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first: the counter almost always wants the bill just entered, not the oldest.
            .OrderByDescending(p => p.InvoiceDate)
            .ThenByDescending(p => p.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> SupplierInvoiceNumberExistsAsync(
        Guid supplierId, string supplierInvoiceNumber, CancellationToken cancellationToken) =>
        _context.Purchases.AnyAsync(
            p => p.SupplierId == supplierId &&
                 p.SupplierInvoiceNumber == supplierInvoiceNumber &&
                 p.Status != PurchaseStatus.Cancelled,
            cancellationToken);

    public async Task AddAsync(Purchase purchase, CancellationToken cancellationToken) =>
        await _context.Purchases.AddAsync(purchase, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
