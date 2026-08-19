using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <remarks>
    /// The role and its permissions come along everywhere. A user without them loaded is a user
    /// whose guards silently answer "no", which is far harder to spot than a missing include.
    /// </remarks>
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Users
            .Include(u => u.Role)
            .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalised = (username ?? string.Empty).Trim().ToLowerInvariant();

        return _context.Users
            .Include(u => u.Role)
            .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(u => u.Username == normalised, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .ThenInclude(r => r!.Permissions)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        _context.Users.AnyAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await _context.Users.AddAsync(user, cancellationToken);

    /// <remarks>The user comes along because every request needs their name and role, not just the id.</remarks>
    public Task<UserSession?> GetSessionAsync(string token, CancellationToken cancellationToken) =>
        _context.UserSessions
            .Include(s => s.User)
            .ThenInclude(u => u!.Role)
            .ThenInclude(r => r!.Permissions)
            .FirstOrDefaultAsync(s => s.Token == token, cancellationToken);

    public async Task AddSessionAsync(UserSession session, CancellationToken cancellationToken) =>
        await _context.UserSessions.AddAsync(session, cancellationToken);

    public Task RemoveSessionAsync(string token, CancellationToken cancellationToken) =>
        _context.UserSessions.Where(s => s.Token == token).ExecuteDeleteAsync(cancellationToken);

    public Task RemoveSessionsForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.UserSessions.Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);

    public async Task AddAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken) =>
        await _context.AuditEvents.AddAsync(auditEvent, cancellationToken);

    public async Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> SearchAuditAsync(
        string? search,
        AuditAction? action,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.AuditEvents.AsNoTracking().AsQueryable();

        if (action is { } a) query = query.Where(e => e.Action == a);

        if (fromDate is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.OccurredAt >= fromUtc);
        }

        if (toDate is { } to)
        {
            var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.OccurredAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.UserName, pattern) ||
                (e.EntityLabel != null && EF.Functions.ILike(e.EntityLabel, pattern)) ||
                (e.Detail != null && EF.Functions.ILike(e.Detail, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
