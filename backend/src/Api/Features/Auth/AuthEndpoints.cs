using Application.DTOs.Auth;
using Application.DTOs.Roles;
using Application.Interfaces;

namespace Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // The one route reachable without a token — see SessionMiddleware.
        group.MapPost("/sign-in", async (
            SignInRequest request, IAuthService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SignInAsync(request, cancellationToken)));

        group.MapPost("/sign-out", async (
            HttpContext http, IAuthService service, CancellationToken cancellationToken) =>
        {
            var header = http.Request.Headers.Authorization.ToString();
            var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..].Trim()
                : string.Empty;

            await service.SignOutAsync(token, cancellationToken);
            return Results.NoContent();
        });

        // Reloaded from the row, not read off the token, so a reset or a deactivation reaches a tab
        // that has been open since before it happened.
        group.MapGet("/me", async (IAuthService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetCurrentAsync(cancellationToken)));

        group.MapPost("/change-password", async (
            ChangePasswordRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.ChangePasswordAsync(request, cancellationToken);
            return Results.NoContent();
        });

        var users = app.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/", async (IAuthService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUsersAsync(cancellationToken)));

        // The generated password comes back exactly once. There is no way to read it again.
        users.MapPost("/", async (
            CreateUserRequest request, IAuthService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CreateUserAsync(request, cancellationToken)));

        users.MapPost("/{id:guid}/reset-password", async (
            Guid id, IAuthService service, CancellationToken cancellationToken) =>
            Results.Ok(new { temporaryPassword = await service.ResetPasswordAsync(id, cancellationToken) }));

        users.MapPost("/{id:guid}/deactivate", async (
            Guid id, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.SetActiveAsync(id, false, cancellationToken);
            return Results.NoContent();
        });

        users.MapPost("/{id:guid}/activate", async (
            Guid id, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.SetActiveAsync(id, true, cancellationToken);
            return Results.NoContent();
        });

        users.MapPut("/{id:guid}/role", async (
            Guid id,
            ChangeUserRoleRequest request,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            await service.ChangeRoleAsync(id, request, cancellationToken);
            return Results.NoContent();
        });

        var roles = app.MapGroup("/api/roles").WithTags("Roles");

        // The permission list is code, so this needs no permission of its own — it says what the
        // app can enforce, not what anybody holds.
        roles.MapGet("/permissions", (IRoleService service) => Results.Ok(service.GetPermissions()));

        roles.MapGet("/", async (IRoleService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetRolesAsync(cancellationToken)));

        roles.MapPost("/", async (
            SaveRoleRequest request, IRoleService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.CreateAsync(request, cancellationToken)));

        roles.MapPut("/{id:guid}", async (
            Guid id, SaveRoleRequest request, IRoleService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(id, request, cancellationToken)));

        roles.MapDelete("/{id:guid}", async (
            Guid id, IRoleService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        var audit = app.MapGroup("/api/audit").WithTags("Audit");

        audit.MapGet("/", async (
            string? search,
            string? action,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? page,
            int? pageSize,
            IAuthService service,
            CancellationToken cancellationToken) =>
        {
            var query = new AuditListQuery(
                search, action, fromDate, toDate,
                page is > 0 ? page.Value : 1, pageSize is > 0 ? pageSize.Value : 50);

            return Results.Ok(await service.GetAuditAsync(query, cancellationToken));
        });
    }
}
