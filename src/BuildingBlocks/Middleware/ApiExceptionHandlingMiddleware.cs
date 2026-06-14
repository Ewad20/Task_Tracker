using System.Net;
using System.Text.Json;
using BuildingBlocks.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Middleware;

public sealed class ApiExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RequestValidationException ex)
        {
            await WriteJsonAsync(context, HttpStatusCode.BadRequest, new
            {
                error = ex.Message,
                errors = ex.Errors,
                traceId = context.TraceIdentifier
            });
        }
        catch (KeyNotFoundException ex)
        {
            await WriteJsonAsync(context, HttpStatusCode.NotFound, new
            {
                error = ex.Message,
                traceId = context.TraceIdentifier
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteJsonAsync(context, HttpStatusCode.Forbidden, new
            {
                error = ex.Message,
                traceId = context.TraceIdentifier
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request exception.");

            await WriteJsonAsync(context, HttpStatusCode.InternalServerError, new
            {
                error = ex.Message,
                traceId = context.TraceIdentifier
            });
        }
    }

    private static async Task WriteJsonAsync(HttpContext context, HttpStatusCode statusCode, object payload)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
