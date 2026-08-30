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

    /// <summary>
    /// Throws unless the caller holds at least one of <paramref name="permissions"/>.
    /// <para>
    /// For the screens more than one role has a reason to open. Whoever closes the day needs to see
    /// the drawer; so does whoever reads the accounts. Requiring either is the honest rule, and
    /// inventing a third permission for the overlap would be a checkbox nobody understands.
    /// </para>
    /// </summary>
    void RequireAny(string action, params Permission[] permissions);
}
