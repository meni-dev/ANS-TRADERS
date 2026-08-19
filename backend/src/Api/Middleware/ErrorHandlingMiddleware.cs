using System.Text.Json;
using Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

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
                "Somebody else changed this while you were working on it. Reload and try again.",
                "CONCURRENT_UPDATE");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", "INTERNAL_ERROR");
        }
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
