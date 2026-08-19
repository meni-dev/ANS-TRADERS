using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? search, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.PartNumber, pattern) ||
                EF.Functions.ILike(p.ItemName, pattern) ||
                (p.VehicleBrand != null && EF.Functions.ILike(p.VehicleBrand, pattern)) ||
                (p.VehicleModel != null && EF.Functions.ILike(p.VehicleModel, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.ItemName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> PartNumberExistsAsync(string partNumber, Guid? excludeId, CancellationToken cancellationToken) =>
        _context.Products.AnyAsync(
            p => p.PartNumber == partNumber && (excludeId == null || p.Id != excludeId), cancellationToken);

    public async Task<IReadOnlyDictionary<string, Product>> GetByPartNumbersAsync(
        IReadOnlyCollection<string> partNumbers, CancellationToken cancellationToken)
    {
        // Tracked: the import updates these in place when the caller asked for that.
        var found = await _context.Products
            .Where(p => partNumbers.Contains(p.PartNumber))
            .ToListAsync(cancellationToken);

        return found.ToDictionary(p => p.PartNumber, StringComparer.OrdinalIgnoreCase);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken) =>
        await _context.Products.AddAsync(product, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
