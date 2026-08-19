using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _context;

    public SupplierRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Supplier> Items, int TotalCount)> SearchAsync(
        string? search, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Suppliers.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.Name, pattern) ||
                EF.Functions.ILike(s.Phone, pattern) ||
                (s.Gstin != null && EF.Functions.ILike(s.Gstin, pattern)) ||
                (s.ContactPerson != null && EF.Functions.ILike(s.ContactPerson, pattern)) ||
                (s.City != null && EF.Functions.ILike(s.City, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> PhoneExistsAsync(string phone, Guid? excludeId, CancellationToken cancellationToken) =>
        _context.Suppliers.AnyAsync(
            s => s.Phone == phone && (excludeId == null || s.Id != excludeId), cancellationToken);

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken) =>
        await _context.Suppliers.AddAsync(supplier, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
