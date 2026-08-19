using Application.Common;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> AnyAsync(CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>The session and its user in one query — every request needs both.</summary>
    Task<UserSession?> GetSessionAsync(string token, CancellationToken cancellationToken);

    Task AddSessionAsync(UserSession session, CancellationToken cancellationToken);

    Task RemoveSessionAsync(string token, CancellationToken cancellationToken);

    /// <summary>Called when somebody is deactivated — their open sessions stop working at once.</summary>
    Task RemoveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> SearchAuditAsync(
        string? search,
        Domain.Enums.AuditAction? action,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
