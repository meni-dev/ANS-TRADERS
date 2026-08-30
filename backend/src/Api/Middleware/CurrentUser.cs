using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Api.Middleware;

/// <summary>
/// Who is making this request. Filled in once by <see cref="SessionMiddleware"/> and read wherever a
/// document needs stamping or a rule needs checking.
/// <para>
/// Scoped, and mutable only from inside this assembly — nothing in Application can pretend to be
/// somebody else or to hold a permission it was not given.
/// </para>
/// </summary>
public class CurrentUser : ICurrentUser
{
    private static readonly IReadOnlySet<Permission> None = new HashSet<Permission>();

    public Guid? UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string RoleName { get; private set; } = string.Empty;

    public IReadOnlySet<Permission> Permissions { get; private set; } = None;

    internal void Set(User user)
    {
        UserId = user.Id;
        Name = user.Name;
        RoleName = user.Role?.Name ?? string.Empty;

        // Copied off the role now rather than read through the navigation later: the set has to
        // describe what the person was allowed to do when the request began, and it must not change
        // underneath a service half-way through a transaction.
        Permissions = user.Role is null
            ? None
            : user.Role.Permissions.Select(p => p.Permission).ToHashSet();
    }

    public bool Has(Permission permission) => Permissions.Contains(permission);

    public void Require(Permission permission, string action)
    {
        if (!Has(permission))
        {
            throw new ForbiddenException($"Your role does not let you {action}");
        }
    }

    public void RequireAny(string action, params Permission[] permissions)
    {
        if (!permissions.Any(Has))
        {
            throw new ForbiddenException($"Your role does not let you {action}");
        }
    }
}
