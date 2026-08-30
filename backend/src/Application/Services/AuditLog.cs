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
        CancellationToken cancellationToken,
        string? actedBy = null) =>
        _repository.AddAuditAsync(
            new AuditEvent
            {
                UserId = _currentUser.UserId,
                // Snapshotted, and "system" when nobody is signed in — the seeder and the backfill
                // command both write without a session, and a blank name reads as a bug.
                //
                // actedBy wins when it is given: at sign-in the person is known but has no session
                // yet, and a trail that says "system signed in" names nobody.
                UserName = actedBy
                    ?? (string.IsNullOrWhiteSpace(_currentUser.Name) ? "system" : _currentUser.Name),
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityLabel = entityLabel,
                Detail = detail,
            },
            cancellationToken);
}
