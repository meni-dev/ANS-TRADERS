using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A named set of permissions the shop puts together itself.
/// <para>
/// Roles are data because every shop divides the work differently — one has a counter boy and a
/// manager, another has a brother who does purchases and nothing else. Permissions are code because
/// each one has to be enforced somewhere.
/// </para>
/// </summary>
public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Set on the role the shop cannot be run without. It holds every permission, cannot be edited
    /// or deleted, and always has at least one person in it — otherwise a wrong click could leave a
    /// shop where nobody can add a user or unlock the books.
    /// </summary>
    public bool IsSystem { get; set; }

    public List<RolePermission> Permissions { get; set; } = [];

    public bool Has(Permission permission) => Permissions.Any(p => p.Permission == permission);
}

/// <summary>
/// One permission granted to one role.
/// <para>
/// A child row rather than a column of text: it makes "who can cancel a bill" a query, and it lets
/// the database refuse the same permission twice on one role.
/// </para>
/// </summary>
public class RolePermission : Entity
{
    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public Permission Permission { get; set; }
}
