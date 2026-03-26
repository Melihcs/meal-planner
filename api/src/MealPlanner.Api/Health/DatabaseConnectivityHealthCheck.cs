namespace MealPlanner.Api.Health;

using MealPlanner.Api.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

public sealed class DatabaseConnectivityHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = PostgresConnectionString.Resolve(configuration);
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL connectivity check failed.",
                exception);
        }
    }
}
