using Application.Common;
using Application.DTOs.Auth;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<SignInResultDto> SignInAsync(SignInRequest request, CancellationToken cancellationToken);

    Task SignOutAsync(string token, CancellationToken cancellationToken);

    /// <summary>The signed-in user, reloaded rather than remembered — a role change or a forced
    /// password reset has to reach a tab that was already open.</summary>
    Task<SignedInUserDto> GetCurrentAsync(CancellationToken cancellationToken);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken);

    Task<CreatedUserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<string> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken);

    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    Task ChangeRoleAsync(Guid userId, ChangeUserRoleRequest request, CancellationToken cancellationToken);

    Task<PagedResult<AuditEventDto>> GetAuditAsync(
        AuditListQuery query, CancellationToken cancellationToken);
}
