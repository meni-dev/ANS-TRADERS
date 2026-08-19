using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class AuthService : IAuthService
{
    /// <summary>
    /// Long enough that a counter is never signed out mid-bill, short enough that a machine left on
    /// overnight is not a way in. Rolled forward on every request, so it measures idleness.
    /// </summary>
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);

    private readonly IUserRepository _repository;
    private readonly IRoleRepository _roles;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public AuthService(
        IUserRepository repository,
        IRoleRepository roles,
        ICurrentUser currentUser,
        IAuditLog audit)
    {
        _repository = repository;
        _roles = roles;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<SignInResultDto> SignInAsync(
        SignInRequest request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByUsernameAsync(request.Username ?? string.Empty, cancellationToken);

        // One message for both a wrong name and a wrong password. Telling them apart would let
        // somebody find out which accounts exist by trying names.
        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Username"] = ["That username and password do not match"],
            });
        }

        var session = new UserSession
        {
            UserId = user.Id,
            Token = NewToken(),
            ExpiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime),
        };

        await _repository.AddSessionAsync(session, cancellationToken);

        user.LastSignedInAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        return new SignInResultDto(session.Token, session.ExpiresAt, ToSignedInDto(user));
    }

    public async Task SignOutAsync(string token, CancellationToken cancellationToken)
    {
        await _repository.RemoveSessionAsync(token, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await RequireSignedInAsync(cancellationToken);

        if (!PasswordHasher.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            throw Invalid("CurrentPassword", "That is not your current password");
        }

        Validate(request.NewPassword);

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Every other session is dropped: a password is usually changed because the old one is no
        // longer trusted, and leaving those open would defeat the point.
        await _repository.RemoveSessionsForUserAsync(user.Id, cancellationToken);

        await _audit.RecordAsync(
            AuditAction.PasswordChanged, "User", user.Id, user.Username, null, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "see who can sign in");

        var users = await _repository.GetAllAsync(cancellationToken);

        return users.Select(ToDto).ToList();
    }

    public async Task<CreatedUserDto> CreateUserAsync(
        CreateUserRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "add someone");

        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();

        if (username.Length < 3)
        {
            throw Invalid("Username", "A username needs at least three characters");
        }

        if (await _repository.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new ConflictException($"'{username}' is already taken", "USERNAME_TAKEN");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw Invalid("Name", "Who is this?");
        }

        var temporary = PasswordHasher.GenerateTemporary();

        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw Invalid("RoleId", "Pick a role for them");

        var user = new User
        {
            Name = request.Name.Trim(),
            Username = username,
            PasswordHash = PasswordHasher.Hash(temporary),
            RoleId = role.Id,
            Role = role,
            MustChangePassword = true,
        };

        await _repository.AddAsync(user, cancellationToken);

        await _audit.RecordAsync(
            AuditAction.UserCreated, "User", user.Id, user.Username,
            role.Name, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return new CreatedUserDto(ToSignedInDto(user), temporary);
    }

    public async Task<string> ResetPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "reset a password");

        var user = await _repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' was not found", "USER_NOT_FOUND");

        var temporary = PasswordHasher.GenerateTemporary();

        user.PasswordHash = PasswordHasher.Hash(temporary);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.RemoveSessionsForUserAsync(user.Id, cancellationToken);

        await _audit.RecordAsync(
            AuditAction.PasswordChanged, "User", user.Id, user.Username, "reset by owner", cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return temporary;
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "change who can sign in");

        var user = await _repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' was not found", "USER_NOT_FOUND");

        if (!isActive && user.Id == _currentUser.UserId)
        {
            throw new ConflictException("You cannot lock yourself out", "CANNOT_DEACTIVATE_SELF");
        }

        // Somebody has to be left who can add people back. Measured on the permission rather than
        // on a role name, because the shop can build a second administrator role and call it
        // anything it likes.
        if (!isActive && Administers(user))
        {
            await RequireAnotherAdministratorAsync(user.Id, cancellationToken);
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!isActive)
        {
            await _repository.RemoveSessionsForUserAsync(user.Id, cancellationToken);
        }

        await _audit.RecordAsync(
            isActive ? AuditAction.UserCreated : AuditAction.UserDeactivated,
            "User", user.Id, user.Username, null, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditEventDto>> GetAuditAsync(
        AuditListQuery query, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.AuditView, "read the audit trail");

        var action = Enum.TryParse<AuditAction>(query.Action, ignoreCase: true, out var parsed)
            ? parsed
            : (AuditAction?)null;

        var (items, totalCount) = await _repository.SearchAuditAsync(
            query.Search, action, query.FromDate, query.ToDate,
            query.Page, query.PageSize, cancellationToken);

        return new PagedResult<AuditEventDto>(
            items.Select(ToDto).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task ChangeRoleAsync(
        Guid userId, ChangeUserRoleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.UserManage, "change what someone can do");

        var user = await _repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' was not found", "USER_NOT_FOUND");

        var role = await _roles.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw Invalid("RoleId", "Pick a role for them");

        if (user.RoleId == role.Id)
        {
            return;
        }

        // Moving the last administrator into a role without UserManage locks everybody out of the
        // people screen for good — the same hole as deactivating them, reached a different way.
        if (Administers(user) && !role.Has(Permission.UserManage))
        {
            await RequireAnotherAdministratorAsync(user.Id, cancellationToken);
        }

        var previous = user.Role?.Name ?? "none";

        user.RoleId = role.Id;
        user.Role = role;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Their open sessions carry the permissions they had when they signed in, so they have to
        // go — otherwise a demotion does not take effect until they happen to sign out.
        await _repository.RemoveSessionsForUserAsync(user.Id, cancellationToken);

        await _audit.RecordAsync(
            AuditAction.UserRoleChanged, "User", user.Id, user.Username,
            $"{previous} to {role.Name}", cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static bool Administers(User user) =>
        user.Role?.Has(Permission.UserManage) == true;

    /// <summary>
    /// Refuses unless somebody other than <paramref name="exceptUserId"/> can still manage people.
    /// </summary>
    private async Task RequireAnotherAdministratorAsync(
        Guid exceptUserId, CancellationToken cancellationToken)
    {
        var others = (await _repository.GetAllAsync(cancellationToken))
            .Count(u => u.IsActive && u.Id != exceptUserId && Administers(u));

        if (others == 0)
        {
            throw new ConflictException(
                "This is the only person who can manage people — give somebody else that first",
                "LAST_ADMINISTRATOR");
        }
    }

    /// <summary>What the action is called on screen.</summary>
    public static string ActionLabel(AuditAction action) => action switch
    {
        AuditAction.Cancelled => "Cancelled a document",
        AuditAction.StockAdjusted => "Adjusted stock",
        AuditAction.DiscountGiven => "Gave a discount",
        AuditAction.BooksLocked => "Locked the books",
        AuditAction.BooksUnlocked => "Unlocked the books",
        AuditAction.UserCreated => "Added or restored a user",
        AuditAction.UserDeactivated => "Deactivated a user",
        AuditAction.PasswordChanged => "Changed a password",
        AuditAction.SignedIn => "Signed in",
        AuditAction.RoleChanged => "Changed a role",
        AuditAction.UserRoleChanged => "Moved someone to another role",
        _ => "Imported a catalogue",
    };

    public async Task<SignedInUserDto> GetCurrentAsync(CancellationToken cancellationToken) =>
        ToSignedInDto(await RequireSignedInAsync(cancellationToken));

    private async Task<User> RequireSignedInAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } id)
        {
            throw new ConflictException("Sign in first", "NOT_SIGNED_IN");
        }

        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("That account no longer exists", "USER_NOT_FOUND");
    }

    private static void Validate(string? password)
    {
        // Length only. Character-class rules push people to "Password1!" and a sticky note; a longer
        // passphrase they can actually remember is worth more on a shop counter.
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw Invalid("NewPassword", "Use at least eight characters");
        }
    }

    private static string NewToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static SignedInUserDto ToSignedInDto(User u) =>
        new(
            u.Id,
            u.Name,
            u.Username,
            u.RoleId,
            u.Role?.Name ?? string.Empty,
            u.MustChangePassword,
            u.Role?.Permissions.Select(p => p.Permission.ToString()).Order().ToList() ?? []);

    private static UserDto ToDto(User u) =>
        new(
            u.Id, u.Name, u.Username, u.RoleId, u.Role?.Name ?? string.Empty,
            u.IsActive, u.MustChangePassword, u.LastSignedInAt);

    private static AuditEventDto ToDto(AuditEvent e) =>
        new(e.Id, e.OccurredAt, e.UserName, e.Action.ToString(), ActionLabel(e.Action),
            e.EntityType, e.EntityId, e.EntityLabel, e.Detail);

    private static ValidationAppException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
