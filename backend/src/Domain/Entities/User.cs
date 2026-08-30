using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Somebody who works the counter.
/// <para>
/// Deliberately not the multi-tenant identity the SaaS spec describes — one shop, a handful of
/// staff. What it exists for is the question nobody could answer before: <b>who took that money</b>.
/// </para>
/// </summary>
public class User : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>What they type to sign in. Unique, case-insensitively.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash and its salt, packed into one string. The password itself is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// What they are allowed to do, by way of the role they hold. Every account has one — there is
    /// no such thing as a signed-in person with no role, because that would be a person the guards
    /// cannot reason about.
    /// </summary>
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    /// <summary>
    /// Set when the account was created with a generated password. The app makes them choose their
    /// own before it lets them do anything else.
    /// </summary>
    public bool MustChangePassword { get; set; }

    public DateTimeOffset? LastSignedInAt { get; set; }

    /// <summary>
    /// Wrong passwords in a row. Reset the moment one works.
    /// </summary>
    public int FailedSignInCount { get; set; }

    /// <summary>
    /// Set once the failures pass the limit, and the account refuses to sign in until it passes.
    /// <para>
    /// Held on the row rather than in memory because the API runs as several copies at once on
    /// Lambda — a counter kept in one process would let an attacker simply spread the guesses.
    /// </para>
    /// </summary>
    public DateTimeOffset? LockedOutUntil { get; set; }

    /// <summary>
    /// Deactivated rather than deleted. Their name is on documents going back years, and a foreign
    /// key that dangles is worse than an account that cannot sign in.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
