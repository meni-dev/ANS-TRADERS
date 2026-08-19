namespace Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Who made it. Null on everything written before the shop had accounts — that history has no
    /// honest answer, and inventing one would be worse than admitting it.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// Their name as it was at the time. Snapshotted alongside the id because the account may later
    /// be renamed or deactivated, and a document should still read as it did when it was made.
    /// </summary>
    public string? CreatedByName { get; set; }
}
