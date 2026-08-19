using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// A signed-in session, held server-side rather than in a self-contained token.
/// <para>
/// A JWT would need no table, but it also cannot be withdrawn: sacking somebody at noon would leave
/// their token working until it expired. For one shop, a row that can be deleted is both simpler and
/// stricter — sign-out and removal take effect on the next request.
/// </para>
/// </summary>
public class UserSession : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>A random 256-bit value, base64url. Stored as issued — it is the credential.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Rolled forward on use, so an active counter is not signed out mid-bill.</summary>
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}
