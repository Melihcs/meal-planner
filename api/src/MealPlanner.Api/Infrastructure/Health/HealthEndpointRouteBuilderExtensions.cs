using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MealPlanner.Api.Infrastructure.Health;

public static class HealthEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointConventionBuilder MapMealPlannerHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapHealthChecks("/health", CreateOptions());
    }

    internal static HealthCheckOptions CreateOptions()
    {
        return new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = WriteResponseAsync,
        };
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new HealthResponse(
            report.Status.ToString(),
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthDependencyResponse(
                    entry.Value.Status.ToString(),
                    entry.Value.Description)));

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private sealed record HealthResponse(
        string Status,
        IReadOnlyDictionary<string, HealthDependencyResponse> Checks);

    private sealed record HealthDependencyResponse(
        string Status,
        string? Description);
}
