using System.Text.Json;
using SupportDesk.Api.Common.Exceptions;

namespace SupportDesk.Api.Common.Middleware;

/// <summary>
/// Centralized error handling: translates known AppExceptions into a consistent
/// { code, message } JSON body, and hides unhandled exceptions behind a generic
/// 500 response so stack traces / EF internals never reach the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (AppException ex)
        {
            _logger.LogInformation(ex, "Request rejected with {Code}", ex.Code);
            await WriteErrorAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new { code, message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
