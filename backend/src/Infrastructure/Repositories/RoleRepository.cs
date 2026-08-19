using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _context;

    public RoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Roles
            .Include(r => r.Permissions)
            // The built-in role first, then alphabetically — a list that reorders itself as roles
            // are renamed is a list nobody can find anything in twice.
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalised = name.Trim();

        return _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == normalised.ToLower(), cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserCountsAsync(
        CancellationToken cancellationToken) =>
        await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken) =>
        await _context.Roles.AddAsync(role, cancellationToken);

    public void Remove(Role role) => _context.Roles.Remove(role);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
