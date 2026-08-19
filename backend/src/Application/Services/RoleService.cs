using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Roles;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public RoleService(IRoleRepository repository, ICurrentUser currentUser, IAuditLog audit)
    {
        _repository = repository;
        _currentUser = currentUser;
        _audit = audit;
    }

    public IReadOnlyList<PermissionDto> GetPermissions() => PermissionCatalogue.All();

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "see the roles");

        var roles = await _repository.GetAllAsync(cancellationToken);
        var counts = await _repository.GetUserCountsAsync(cancellationToken);

        return roles.Select(r => ToDto(r, counts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "create a role");

        var name = Name(request);

        if (await _repository.GetByNameAsync(name, cancellationToken) is not null)
        {
            throw new ConflictException($"There is already a role called '{name}'", "ROLE_NAME_TAKEN");
        }

        var role = new Role
        {
            Name = name,
            Description = Clean(request.Description),
        };

        Apply(role, request);

        await _repository.AddAsync(role, cancellationToken);

        await _audit.RecordAsync(
            AuditAction.RoleChanged, "Role", role.Id, role.Name,
            $"created with {role.Permissions.Count} permissions", cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDto(role, 0);
    }

    public async Task<RoleDto> UpdateAsync(
        Guid id, SaveRoleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "change a role");

        var role = await Load(id, cancellationToken);

        // The system role holds everything and cannot be trimmed. Without this, one wrong tick
        // could leave a shop where nobody can add a user, unlock the books, or put it back.
        if (role.IsSystem)
        {
            throw new ConflictException(
                $"'{role.Name}' is the built-in role and cannot be changed. Make a new role instead.",
                "ROLE_IS_SYSTEM");
        }

        var name = Name(request);

        if (await _repository.GetByNameAsync(name, cancellationToken) is { } clash && clash.Id != role.Id)
        {
            throw new ConflictException($"There is already a role called '{name}'", "ROLE_NAME_TAKEN");
        }

        var before = role.Permissions.Select(p => p.Permission).ToHashSet();

        role.Name = name;
        role.Description = Clean(request.Description);
        role.UpdatedAt = DateTimeOffset.UtcNow;

        Apply(role, request);

        var after = role.Permissions.Select(p => p.Permission).ToHashSet();

        // What changed, not what it now holds. "Added BillCancel" is the line somebody needs months
        // later; a list of twenty names says nothing about what somebody decided today.
        var added = after.Except(before).Select(p => $"+{p}");
        var removed = before.Except(after).Select(p => $"-{p}");
        var change = string.Join(" ", added.Concat(removed));

        await _audit.RecordAsync(
            AuditAction.RoleChanged, "Role", role.Id, role.Name,
            change.Length == 0 ? "renamed" : change, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var counts = await _repository.GetUserCountsAsync(cancellationToken);

        return ToDto(role, counts.GetValueOrDefault(role.Id));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "delete a role");

        var role = await Load(id, cancellationToken);

        if (role.IsSystem)
        {
            throw new ConflictException(
                $"'{role.Name}' is the built-in role and cannot be deleted", "ROLE_IS_SYSTEM");
        }

        var counts = await _repository.GetUserCountsAsync(cancellationToken);

        // Move the people first. Deleting the role under them would leave accounts pointing at
        // nothing, and the guards would have no answer for what those people may do.
        if (counts.GetValueOrDefault(role.Id) > 0)
        {
            throw new ConflictException(
                $"{counts[role.Id]} people still hold '{role.Name}'. Move them to another role first.",
                "ROLE_IN_USE");
        }

        _repository.Remove(role);

        await _audit.RecordAsync(
            AuditAction.RoleChanged, "Role", role.Id, role.Name, "deleted", cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> Load(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Role '{id}' was not found", "ROLE_NOT_FOUND");

    private static string Name(SaveRoleRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();

        if (name.Length < 2)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Name"] = ["Give the role a name people will recognise"],
            });
        }

        return name;
    }

    private static void Apply(Role role, SaveRoleRequest request)
    {
        var wanted = PermissionCatalogue.Parse(request.Permissions);

        if (wanted.Count == 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                // A role holding nothing is an account that can sign in and then do nothing at all,
                // which reads as the app being broken rather than as a decision somebody made.
                ["Permissions"] = ["Tick at least one thing this role can do"],
            });
        }

        role.Permissions.RemoveAll(p => !wanted.Contains(p.Permission));

        foreach (var permission in wanted.Where(p => role.Permissions.All(x => x.Permission != p)))
        {
            role.Permissions.Add(new RolePermission { RoleId = role.Id, Permission = permission });
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RoleDto ToDto(Role role, int userCount) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            role.Permissions.Select(p => p.Permission.ToString()).Order().ToList(),
            userCount);
}
