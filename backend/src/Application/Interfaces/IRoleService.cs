using Application.DTOs.Roles;

namespace Application.Interfaces;

public interface IRoleService
{
    /// <summary>Every permission this build enforces, grouped for the screen.</summary>
    IReadOnlyList<PermissionDto> GetPermissions();

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken);

    Task<RoleDto> CreateAsync(SaveRoleRequest request, CancellationToken cancellationToken);

    Task<RoleDto> UpdateAsync(Guid id, SaveRoleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
