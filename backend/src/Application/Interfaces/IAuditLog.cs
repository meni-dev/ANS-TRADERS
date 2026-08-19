using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Records the actions that rewrite or hide something.
/// <para>
/// Follows the house rule the ledgers follow: <b>it does not save</b>. The caller saves, so the
/// audit row commits in the same transaction as the thing it describes — a cancellation that
/// succeeded with no log entry, or a log entry for a cancellation that rolled back, would both be
/// worse than no log at all.
/// </para>
/// </summary>
public interface IAuditLog
{
    Task RecordAsync(
        AuditAction action,
        string entityType,
        Guid? entityId,
        string? entityLabel,
        string? detail,
        CancellationToken cancellationToken);
}
