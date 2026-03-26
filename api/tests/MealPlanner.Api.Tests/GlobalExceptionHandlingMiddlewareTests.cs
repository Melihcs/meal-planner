using System.Text.Json;
using MealPlanner.Api.Domain.Exceptions;
using MealPlanner.Api.Infrastructure.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace MealPlanner.Api.Tests;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_MapsNotFoundExceptionTo404ProblemDetails()
    {
        var middleware = CreateMiddleware(
            () => throw new NotFoundException("Recipe was not found."));
        var httpContext = CreateHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Resource not found", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal("Recipe was not found.", payload.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_MapsValidationExceptionTo400ProblemDetails()
    {
        var middleware = CreateMiddleware(
            () => throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["title"] = ["Title is required."],
                }));
        var httpContext = CreateHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Validation failed", payload.RootElement.GetProperty("title").GetString());
        Assert.True(
            payload.RootElement.GetProperty("errors").GetProperty("title")[0].GetString() is
                "Title is required.");
    }

    [Fact]
    public async Task InvokeAsync_MapsUnauthorizedAccessExceptionTo401ProblemDetails()
    {
        var middleware = CreateMiddleware(
            () => throw new UnauthorizedAccessException("Authentication is required."));
        var httpContext = CreateHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);

        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Unauthorized", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Authentication is required.",
            payload.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task InvokeAsync_MapsUnexpectedExceptionsTo500ProblemDetails()
    {
        var middleware = CreateMiddleware(
            () => throw new InvalidOperationException("Unexpected failure."));
        var httpContext = CreateHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("An unexpected error occurred", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal("Unexpected failure.", payload.RootElement.GetProperty("detail").GetString());
        Assert.True(payload.RootElement.TryGetProperty("traceId", out _));
    }

    private static GlobalExceptionHandlingMiddleware CreateMiddleware(Action action)
    {
        RequestDelegate next = _ =>
        {
            action();
            return Task.CompletedTask;
        };

        return new GlobalExceptionHandlingMiddleware(
            next,
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance,
            new TestHostEnvironment());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Get,
                Path = "/test",
            },
            Response =
            {
                Body = new MemoryStream(),
            },
        };
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(httpContext.Response.Body);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "MealPlanner.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
