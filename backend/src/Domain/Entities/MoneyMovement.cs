using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Money in or out that belongs to nobody — see <see cref="MoneyMovementKind"/>.
/// <para>
/// Deliberately not a <c>Payment</c>: a payment has a party, a receipt number and a balance it
/// moves. None of that is true here, and forcing it would put a customer's name on the owner's own
/// money.
/// </para>
/// </summary>
public class MoneyMovement : AuditableEntity
{
    public DateOnly MovementDate { get; set; }

    public MoneyMovementKind Kind { get; set; }

    /// <summary>Always positive. Which way it moves is the kind's business, not the sign's.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether the till was the side that changed. A capital introduction paid straight into the
    /// bank never passes through the drawer, and counting it there would make the day close wrong.
    /// </summary>
    public bool AffectsCash { get; set; } = true;

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    /// <summary>Flagged, never deleted — the same rule every other document here follows.</summary>
    public bool IsCancelled { get; set; }
}
