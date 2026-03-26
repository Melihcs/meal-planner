namespace MealPlanner.Api.Infrastructure.Configuration;

using Npgsql;

public static class PostgresConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var configuredConnectionString = configuration.GetConnectionString("Postgres");
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertDatabaseUrlToConnectionString(databaseUrl);
        }

        throw new InvalidOperationException(
            "A PostgreSQL connection string must be configured via ConnectionStrings:Postgres or DATABASE_URL.");
    }

    private static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1
            ? Uri.UnescapeDataString(userInfo[1])
            : string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = username,
            Password = password,
        };

        return builder.ConnectionString;
    }
}
