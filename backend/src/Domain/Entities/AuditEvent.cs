using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// An immutable record of something worth being able to ask about later.
/// <para>
/// Not every write lands here — documents already carry their own history, and a log of everything
/// is a log nobody reads. What lands here is the set of actions that <b>rewrite or hide</b>: a
/// cancellation, a stock adjustment, a discount, a books lock, a password change. Those are the ones
/// somebody eventually has to explain.
/// </para>
/// </summary>
public class AuditEvent : Entity
{
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid? UserId { get; set; }

    /// <summary>Snapshotted: the account may later be renamed or deactivated.</summary>
    public string UserName { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    /// <summary>What kind of thing it happened to — <c>Invoice</c>, <c>Product</c>, <c>DayClose</c>.</summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    /// <summary>The document number or name, so the log reads without a join.</summary>
    public string? EntityLabel { get; set; }

    /// <summary>What changed, or why — free text, written for a human reading it in a year.</summary>
    public string? Detail { get; set; }
}
