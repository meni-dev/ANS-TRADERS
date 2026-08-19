namespace Application.DTOs.Auth;

public record SignInRequest(string Username, string Password);

/// <summary>
/// <paramref name="Permissions"/> travels with the user so the screen can hide doors it cannot
/// open. It is a convenience, not a control — the server refuses the action either way, and a
/// hidden button protects nothing on its own.
/// </summary>
public record SignedInUserDto(
    Guid Id,
    string Name,
    string Username,
    Guid RoleId,
    string RoleName,
    bool MustChangePassword,
    IReadOnlyList<string> Permissions);

public record SignInResultDto(string Token, DateTimeOffset ExpiresAt, SignedInUserDto User);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record CreateUserRequest(string Name, string Username, Guid RoleId);

public record ChangeUserRoleRequest(Guid RoleId);

/// <summary>
/// The generated password is returned exactly once, on creation. It is never stored in the clear and
/// there is no way to read it back — a forgotten one is reset, not recovered.
/// </summary>
public record CreatedUserDto(SignedInUserDto User, string TemporaryPassword);

public record UserDto(
    Guid Id,
    string Name,
    string Username,
    Guid RoleId,
    string RoleName,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LastSignedInAt);

public record AuditEventDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    string UserName,
    string Action,
    string ActionLabel,
    string EntityType,
    Guid? EntityId,
    string? EntityLabel,
    string? Detail);

public record AuditListQuery(
    string? Search,
    string? Action,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 50);
