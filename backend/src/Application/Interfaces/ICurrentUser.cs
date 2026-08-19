using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Who is making the current request, and what they are allowed to do. Resolved once per request
/// from the session token and injected wherever a document needs stamping or a rule needs checking.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>Empty when nobody is signed in — the backfill command and the seeder run that way.</summary>
    string Name { get; }

    /// <summary>The role's name, for display and for the audit trail. Empty when not signed in.</summary>
    string RoleName { get; }

    IReadOnlySet<Permission> Permissions { get; }

    bool Has(Permission permission);

    /// <summary>
    /// Throws when the caller does not hold <paramref name="permission"/>.
    /// <para>
    /// <paramref name="action"/> names the thing being attempted in plain words, because "Forbidden"
    /// makes somebody think the app is broken while "you cannot cancel a bill" tells them who to ask.
    /// </para>
    /// </summary>
    void Require(Permission permission, string action);
}
