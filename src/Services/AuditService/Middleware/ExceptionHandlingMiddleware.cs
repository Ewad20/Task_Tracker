using System.Net;
using System.Text.Json;

namespace AuditService.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new
            {
                error = ex.Message,
                traceId = context.TraceIdentifier
            });
            await context.Response.WriteAsync(payload);
        }
    }
}
