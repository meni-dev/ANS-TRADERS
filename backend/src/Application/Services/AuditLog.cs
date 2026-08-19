using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class AuditLog : IAuditLog
{
    private readonly IUserRepository _repository;
    private readonly ICurrentUser _currentUser;

    public AuditLog(IUserRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public Task RecordAsync(
        AuditAction action,
        string entityType,
        Guid? entityId,
        string? entityLabel,
        string? detail,
        CancellationToken cancellationToken) =>
        _repository.AddAuditAsync(
            new AuditEvent
            {
                UserId = _currentUser.UserId,
                // Snapshotted, and "system" when nobody is signed in — the seeder and the backfill
                // command both write without a session, and a blank name reads as a bug.
                UserName = string.IsNullOrWhiteSpace(_currentUser.Name) ? "system" : _currentUser.Name,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityLabel = entityLabel,
                Detail = detail,
            },
            cancellationToken);
}
