using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> SearchAsync(
        string? search, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                EF.Functions.ILike(c.Phone, pattern) ||
                (c.Gstin != null && EF.Functions.ILike(c.Gstin, pattern)) ||
                (c.City != null && EF.Functions.ILike(c.City, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> PhoneExistsAsync(string phone, Guid? excludeId, CancellationToken cancellationToken) =>
        _context.Customers.AnyAsync(
            c => c.Phone == phone && (excludeId == null || c.Id != excludeId), cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken) =>
        await _context.Customers.AddAsync(customer, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
