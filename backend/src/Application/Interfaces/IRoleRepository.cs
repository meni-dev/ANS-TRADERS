using Domain.Entities;

namespace Application.Interfaces;

public interface IRoleRepository
{
    /// <summary>Roles with their permissions loaded — the screen never wants one without the other.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken);

    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Active people per role, so a role in use cannot be deleted out from under them.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetUserCountsAsync(CancellationToken cancellationToken);

    Task AddAsync(Role role, CancellationToken cancellationToken);

    void Remove(Role role);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
