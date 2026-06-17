using BuildingBlocks.Middleware;
using BuildingBlocks.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildingBlocks.Tests.Middleware;

public class ApiExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ApiExceptionHandlingMiddleware Create(RequestDelegate next)
        => new(next, NullLogger<ApiExceptionHandlingMiddleware>.Instance);

    [Fact]
    public async Task InvokeAsync_WhenNoException_Returns200()
    {
        var context = CreateContext();
        var middleware = Create(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_Returns404()
    {
        var context = CreateContext();
        var middleware = Create(_ => throw new KeyNotFoundException("not found"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns403()
    {
        var context = CreateContext();
        var middleware = Create(_ => throw new UnauthorizedAccessException("forbidden"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestValidationException_Returns400()
    {
        var errors = new[] { new RequestValidationError("Field", "Required") };
        var context = CreateContext();
        var middleware = Create(_ => throw new RequestValidationException(errors));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_Returns500()
    {
        var context = CreateContext();
        var middleware = Create(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ResponseBodyIsEmpty()
    {
        var context = CreateContext();
        var middleware = Create(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Body.Length.Should().Be(0);
    }
}
