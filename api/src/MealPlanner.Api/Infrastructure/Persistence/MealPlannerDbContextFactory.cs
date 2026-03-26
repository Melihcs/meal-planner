namespace MealPlanner.Api.Infrastructure.Persistence;

using MealPlanner.Api.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public sealed class MealPlannerDbContextFactory : IDesignTimeDbContextFactory<MealPlannerDbContext>
{
    public MealPlannerDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var appSettingsPath = Path.Combine(basePath, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            var candidate = Path.Combine(basePath, "src", "MealPlanner.Api");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                basePath = candidate;
            }
        }

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MealPlannerDbContext>();
        optionsBuilder.UseNpgsql(PostgresConnectionString.Resolve(configuration));
        optionsBuilder.AddInterceptors(new UpdatedAtSaveChangesInterceptor());

        return new MealPlannerDbContext(optionsBuilder.Options);
    }
}
