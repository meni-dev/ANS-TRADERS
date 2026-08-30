using System.Text.Json;
using Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationAppException ex)
        {
            await WriteResponse(context, StatusCodes.Status400BadRequest, "Validation failed", "VALIDATION_ERROR", ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await WriteResponse(context, StatusCodes.Status404NotFound, ex.Message, ex.Code);
        }
        catch (ForbiddenException ex)
        {
            await WriteResponse(context, StatusCodes.Status403Forbidden, ex.Message, ex.Code);
        }
        catch (ConflictException ex)
        {
            await WriteResponse(context, StatusCodes.Status409Conflict, ex.Message, ex.Code);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Two people settled the same bill at once. The row version caught it, so nobody's
            // figure was silently overwritten — the caller reloads and decides again.
            // Name the rows. Without this the log says only that some row moved, which is the one
            // detail that makes a concurrency report actionable.
            _logger.LogWarning(
                ex,
                "Concurrent update rejected on {Entities}",
                string.Join(", ", ex.Entries.Select(e => e.Metadata.Name)));

            await WriteResponse(
                context,
                StatusCodes.Status409Conflict,
                ConcurrencyMessage(context.Request.Path),
                "CONCURRENT_UPDATE");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } unique)
        {
            // A uniqueness rule the database enforced and the code did not catch first. The
            // document numbering race that used to land here is fixed, and this stays as the net:
            // whatever collides next should tell the counter to try again, not read as the app
            // falling over.
            _logger.LogWarning(ex, "Unique constraint {Constraint} rejected a write", unique.ConstraintName);

            await WriteResponse(
                context,
                StatusCodes.Status409Conflict,
                "Two people saved at the same moment. Nothing was recorded — try again.",
                "DUPLICATE_KEY");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", "INTERNAL_ERROR");
        }
    }

    /// <summary>
    /// What to tell the counter when two people saved the same thing at once.
    /// <para>
    /// The row version already did its job — nothing was overwritten and nothing was recorded
    /// twice. What is left is to say so in terms of the thing on their screen. "Somebody else
    /// changed this" is true of every one of these and useful for none of them: the person is
    /// holding a bill, or standing at a shelf, and needs to know which.
    /// </para>
    /// <para>
    /// Every message says the same three things in the same order — what happened, that nothing was
    /// double-counted, and what to do — because at a counter the real question is always "did it go
    /// through twice?".
    /// </para>
    /// </summary>
    /// <summary>
    /// What to tell the counter when two people saved the same thing at once.
    /// <para>
    /// The row version already did its job — nothing was overwritten and nothing was recorded
    /// twice. What is left is to say so in terms of the thing on their screen. "Somebody else
    /// changed this" is true of every one of these and useful for none of them: the person is
    /// holding a bill, or standing at a shelf, and needs to know which.
    /// </para>
    /// <para>
    /// It reads the route rather than the clashing rows. EF reports only the row whose version
    /// moved, and for a save that touches several that is usually the party balance — so cancelling
    /// a bill would announce that "an account changed" and send somebody to the wrong screen. The
    /// route is the one thing that always says what was actually being done.
    /// </para>
    /// <para>
    /// Every message says the same three things in the same order — what happened, that nothing was
    /// double-counted, and what to do — because at a counter the real question is always "did it go
    /// through twice?".
    /// </para>
    /// </summary>
    private static string ConcurrencyMessage(PathString path)
    {
        var route = path.Value ?? string.Empty;

        var subject = route switch
        {
            _ when route.Contains("/api/credit-notes", StringComparison.OrdinalIgnoreCase)
                => "Somebody else saved this credit note a moment before you. Nothing was returned twice",
            _ when route.Contains("/api/debit-notes", StringComparison.OrdinalIgnoreCase)
                => "Somebody else saved this debit note a moment before you. Nothing was returned twice",
            _ when route.Contains("/api/payments", StringComparison.OrdinalIgnoreCase)
                || route.Contains("/api/cheques", StringComparison.OrdinalIgnoreCase)
                => "Somebody else recorded a payment against this a moment before you. Nothing was collected twice",
            _ when route.Contains("/api/invoices", StringComparison.OrdinalIgnoreCase)
                => "Somebody else saved this bill a moment before you. Nothing was billed twice",
            _ when route.Contains("/api/purchases", StringComparison.OrdinalIgnoreCase)
                => "Somebody else saved this purchase a moment before you. Nothing was recorded twice",
            _ when route.Contains("/api/stock", StringComparison.OrdinalIgnoreCase)
                => "Somebody else changed this part's count a moment before you. Nothing was counted twice",
            _ when route.Contains("/api/products", StringComparison.OrdinalIgnoreCase)
                => "Somebody else changed this part a moment before you. Nothing was saved twice",
            _ when route.Contains("/api/customers", StringComparison.OrdinalIgnoreCase)
                || route.Contains("/api/suppliers", StringComparison.OrdinalIgnoreCase)
                => "Somebody else changed this account a moment before you. Nothing was recorded twice",
            _ when route.Contains("/api/cash", StringComparison.OrdinalIgnoreCase)
                || route.Contains("/api/money", StringComparison.OrdinalIgnoreCase)
                => "Somebody else changed the drawer a moment before you. Nothing was counted twice",
            _ when route.Contains("/api/expenses", StringComparison.OrdinalIgnoreCase)
                => "Somebody else saved this spend a moment before you. Nothing was recorded twice",
            _ when route.Contains("/api/settings", StringComparison.OrdinalIgnoreCase)
                => "Somebody else changed the shop settings a moment before you. Nothing was lost",
            _ => "Somebody else saved this a moment before you. Nothing was recorded twice",
        };

        return subject + " — open the screen again to see where things stand, then try once more.";
    }

    private static Task WriteResponse(
        HttpContext context, int statusCode, string message, string code, object? errors = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var payload = new
        {
            success = false,
            message,
            code,
            errors = errors ?? Array.Empty<string>()
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
