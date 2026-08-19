namespace Application.DTOs.Roles;

/// <summary>
/// One permission as the roles screen shows it: what it is called, which box it sits in, and one
/// line saying what somebody holding it can actually do.
/// </summary>
public record PermissionDto(string Value, string Label, string Group, string Description);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    /// <summary>The role the shop cannot be run without. It cannot be edited or deleted.</summary>
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    /// <summary>How many active people hold it — a role in use cannot simply be deleted.</summary>
    int UserCount);

public record SaveRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);
