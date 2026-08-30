using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Roles;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace UnitTests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(params Permission[] permissions)
    {
        Permissions = permissions.ToHashSet();
    }

    public Guid? UserId { get; set; } = Guid.NewGuid();

    public string Name => "test";

    public string RoleName => "test";

    public IReadOnlySet<Permission> Permissions { get; }

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

internal sealed class FakeRoleRepository : IRoleRepository
{
    public List<Role> Roles { get; } = [];

    public Dictionary<Guid, int> UserCounts { get; } = [];

    public bool Saved { get; private set; }

    public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Role>>(Roles);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Roles.FirstOrDefault(r => r.Id == id));

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(Roles.FirstOrDefault(
            r => string.Equals(r.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyDictionary<Guid, int>> GetUserCountsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(UserCounts);

    public Task AddAsync(Role role, CancellationToken cancellationToken)
    {
        Roles.Add(role);
        return Task.CompletedTask;
    }

    public void Remove(Role role) => Roles.Remove(role);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saved = true;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingAuditLog : IAuditLog
{
    public List<(AuditAction Action, string EntityType, string? Label, string? Detail)> Entries { get; } = [];

    public Task RecordAsync(
        AuditAction action, string entityType, Guid? entityId, string? entityLabel,
        string? detail, CancellationToken cancellationToken, string? actedBy = null)
    {
        Entries.Add((action, entityType, entityLabel, detail));
        return Task.CompletedTask;
    }
}

public class PermissionCatalogueTests
{
    /// <summary>
    /// A permission with no entry here would be enforced by the code and invisible on the roles
    /// screen — nobody could ever grant it, and the feature behind it would look broken.
    /// </summary>
    [Fact]
    public void Every_permission_is_described_for_the_roles_screen()
    {
        var described = PermissionCatalogue.All().Select(p => p.Value).ToHashSet();

        var missing = Enum.GetNames<Permission>().Where(name => !described.Contains(name)).ToList();

        Assert.Empty(missing);
    }

    [Fact]
    public void Every_described_permission_carries_a_group_and_a_sentence()
    {
        foreach (var entry in PermissionCatalogue.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Label), entry.Value);
            Assert.False(string.IsNullOrWhiteSpace(entry.Group), entry.Value);
            Assert.False(string.IsNullOrWhiteSpace(entry.Description), entry.Value);
        }
    }

    /// <summary>
    /// An unrecognised name is a permission this build does not enforce. Storing it would put a row
    /// in the database that looks like a grant and stops nothing.
    /// </summary>
    [Fact]
    public void Unknown_names_are_dropped_rather_than_stored()
    {
        var parsed = PermissionCatalogue.Parse(["BillCreate", "FlyThePlane", "billcancel", "BillCreate"]);

        Assert.Equal([Permission.BillCreate, Permission.BillCancel], parsed);
    }
}

public class RoleServiceTests
{
    private static Role SystemRole() => new()
    {
        Name = "Owner",
        IsSystem = true,
        Permissions = Enum.GetValues<Permission>()
            .Select(p => new RolePermission { Permission = p })
            .ToList(),
    };

    private static Role Staff() => new()
    {
        Name = "Counter Staff",
        Permissions = [new RolePermission { Permission = Permission.BillCreate }],
    };

    private static (RoleService Service, FakeRoleRepository Repository, RecordingAuditLog Audit) Build(
        ICurrentUser currentUser, params Role[] roles)
    {
        var repository = new FakeRoleRepository();
        repository.Roles.AddRange(roles);
        var audit = new RecordingAuditLog();
        return (new RoleService(repository, currentUser, audit), repository, audit);
    }

    [Fact]
    public async Task Someone_without_UserManage_cannot_even_read_the_roles()
    {
        var (service, _, _) = Build(new FakeCurrentUser(Permission.BillCreate), Staff());

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetRolesAsync(CancellationToken.None));
    }

    /// <summary>
    /// The whole point of the built-in role: one wrong tick must not be able to leave a shop where
    /// nobody can add a user, unlock the books, or put it back.
    /// </summary>
    [Fact]
    public async Task The_built_in_role_cannot_be_trimmed()
    {
        var owner = SystemRole();
        var (service, _, _) = Build(new FakeCurrentUser(Permission.UserManage), owner);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(owner.Id, new SaveRoleRequest("Owner", null, ["BillCreate"]), CancellationToken.None));

        Assert.Equal("ROLE_IS_SYSTEM", error.Code);
    }

    [Fact]
    public async Task The_built_in_role_cannot_be_deleted()
    {
        var owner = SystemRole();
        var (service, _, _) = Build(new FakeCurrentUser(Permission.UserManage), owner);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteAsync(owner.Id, CancellationToken.None));
    }

    /// <summary>
    /// Deleting a role out from under its people would leave accounts pointing at nothing, and the
    /// guards would have no answer for what those people may do.
    /// </summary>
    [Fact]
    public async Task A_role_somebody_still_holds_cannot_be_deleted()
    {
        var staff = Staff();
        var (service, repository, _) = Build(new FakeCurrentUser(Permission.UserManage), staff);
        repository.UserCounts[staff.Id] = 2;

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            service.DeleteAsync(staff.Id, CancellationToken.None));

        Assert.Equal("ROLE_IN_USE", error.Code);
        Assert.Contains(staff, repository.Roles);
    }

    [Fact]
    public async Task An_empty_role_is_refused()
    {
        var (service, _, _) = Build(new FakeCurrentUser(Permission.UserManage));

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateAsync(new SaveRoleRequest("Nobody", null, []), CancellationToken.None));
    }

    [Fact]
    public async Task Two_roles_cannot_share_a_name()
    {
        var staff = Staff();
        var (service, _, _) = Build(new FakeCurrentUser(Permission.UserManage), staff);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(
                new SaveRoleRequest("counter staff", null, ["BillCreate"]), CancellationToken.None));

        Assert.Equal("ROLE_NAME_TAKEN", error.Code);
    }

    /// <summary>
    /// The log records what changed, not what the role now holds. "Added BillCancel" is the line
    /// somebody needs months later; a list of twenty names says nothing about today's decision.
    /// </summary>
    [Fact]
    public async Task Changing_a_role_logs_the_difference_not_the_whole_set()
    {
        var staff = Staff();
        var (service, _, audit) = Build(new FakeCurrentUser(Permission.UserManage), staff);

        await service.UpdateAsync(
            staff.Id,
            new SaveRoleRequest("Counter Staff", null, ["BillCancel", "StockView"]),
            CancellationToken.None);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.RoleChanged, entry.Action);
        Assert.Contains("+BillCancel", entry.Detail);
        Assert.Contains("+StockView", entry.Detail);
        Assert.Contains("-BillCreate", entry.Detail);
    }

    [Fact]
    public async Task Permissions_removed_from_a_role_are_actually_dropped()
    {
        var staff = Staff();
        var (service, _, _) = Build(new FakeCurrentUser(Permission.UserManage), staff);

        var result = await service.UpdateAsync(
            staff.Id,
            new SaveRoleRequest("Counter Staff", null, ["StockView"]),
            CancellationToken.None);

        Assert.Equal(["StockView"], result.Permissions);
        Assert.False(staff.Has(Permission.BillCreate));
    }
}
