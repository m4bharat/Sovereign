using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Sovereign.API.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Domain/lookup error.");
            await WriteProblemAsync(context, HttpStatusCode.NotFound, "Not Found", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error.");
            await WriteProblemAsync(context, HttpStatusCode.Conflict, "Database Conflict", "A persistence conflict occurred.");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation/domain argument error.");
            await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Bad Request", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
