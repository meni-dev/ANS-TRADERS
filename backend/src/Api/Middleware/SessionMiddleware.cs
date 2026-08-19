using Application.Interfaces;

namespace Api.Middleware;

/// <summary>
/// Turns the bearer token on a request into the person behind it, and refuses anything that needs a
/// signature and does not have one.
/// <para>
/// Guarding here rather than per-endpoint means a route added later is protected by default. A new
/// endpoint that quietly needs no sign-in is exactly the hole this feature exists to close.
/// </para>
/// </summary>
public class SessionMiddleware
{
    /// <summary>The only paths reachable without signing in.</summary>
    private static readonly string[] Anonymous =
    [
        "/api/auth/sign-in",
        "/health",
        "/swagger",
    ];

    private readonly RequestDelegate _next;

    public SessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository users, ICurrentUser currentUser)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // The browser's pre-flight carries no Authorization header by design, so refusing it would
        // break every cross-origin call before the real request was ever made.
        if (HttpMethods.IsOptions(context.Request.Method)
            || Anonymous.Any(a => path.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var token = Token(context.Request);

        if (token is not null && await users.GetSessionAsync(token, context.RequestAborted) is { } session)
        {
            if (session.ExpiresAt > DateTimeOffset.UtcNow && session.User is { IsActive: true } user)
            {
                ((CurrentUser)currentUser).Set(user);

                // Rolled forward so the session measures idleness rather than age — a counter in use
                // all day is never signed out mid-bill. Written at most once a minute so a busy
                // shop is not making a write on every request.
                if (DateTimeOffset.UtcNow - session.LastSeenAt > TimeSpan.FromMinutes(1))
                {
                    session.LastSeenAt = DateTimeOffset.UtcNow;
                    session.ExpiresAt = DateTimeOffset.UtcNow.AddHours(12);
                    await users.SaveChangesAsync(context.RequestAborted);
                }

                await _next(context);
                return;
            }

            // Expired, or the account was deactivated while it was open. Either way the row is dead.
            await users.RemoveSessionAsync(token, context.RequestAborted);
            await users.SaveChangesAsync(context.RequestAborted);
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            """{"success":false,"message":"Sign in to continue","code":"NOT_SIGNED_IN","errors":[]}""");
    }

    private static string? Token(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;
    }
}
