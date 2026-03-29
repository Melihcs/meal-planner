using MealPlanner.Api.Domain.Exceptions;
using MealPlanner.Api.Features.Recipes;
using MealPlanner.Api.Infrastructure.Auth;
using MealPlanner.Api.Health;
using MealPlanner.Api.Infrastructure.Configuration;
using MealPlanner.Api.Infrastructure.Health;
using MealPlanner.Api.Infrastructure.Http;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var postgresConnectionString = PostgresConnectionString.Resolve(builder.Configuration);
var hasGoogleOAuthConfiguration =
    !string.IsNullOrWhiteSpace(builder.Configuration["GOOGLE_CLIENT_ID"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["GOOGLE_CLIENT_SECRET"]);
var developmentCorsOrigins = new[]
{
    "http://localhost:4200",
    "http://127.0.0.1:4200",
    "http://localhost:4201",
    "http://127.0.0.1:4201",
};

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<UpdatedAtSaveChangesInterceptor>();
builder.Services.AddDbContext<MealPlannerDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(postgresConnectionString);
    options.AddInterceptors(serviceProvider.GetRequiredService<UpdatedAtSaveChangesInterceptor>());
});
builder.Services.AddMealPlannerAuthentication(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDevelopment", policy =>
    {
        policy.WithOrigins(developmentCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseConnectivityHealthCheck>("postgres");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MealPlannerDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCors("FrontendDevelopment");
app.UseAuthentication();
app.UseAuthorization();
var api = app.MapGroup(string.Empty)
    .AddEndpointFilterFactory(RequestValidationEndpointFilterFactory.Create);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    api.MapPost("/auth/dev-login", async (
            DevelopmentAuthLoginService developmentAuthLoginService,
            CancellationToken cancellationToken) =>
        {
            var token = await developmentAuthLoginService.SignInAsync(cancellationToken);
            return Results.Ok(new { token });
        })
        .WithName("BeginDevelopmentAuth");
}

api.MapMealPlannerHealthEndpoint();
api.MapGet("/auth/google", (string? redirect_uri, RedirectUriValidator redirectUriValidator) =>
    {
        if (!hasGoogleOAuthConfiguration)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Google OAuth is not configured",
                detail: "Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET to enable Google sign-in.");
        }

        var validatedRedirectUri = redirectUriValidator.Validate(redirect_uri);
        var authenticationProperties = new AuthenticationProperties();
        authenticationProperties.Items["redirect_uri"] = validatedRedirectUri.ToString();

        return Results.Challenge(authenticationProperties, [GoogleDefaults.AuthenticationScheme]);
    })
    .WithName("BeginGoogleOAuth");

var protectedApi = api.MapGroup(string.Empty)
    .RequireAuthorization();

protectedApi.MapGet("/auth/me", (ICurrentUserService currentUser) => Results.Ok(new
{
    userId = currentUser.UserId,
    email = currentUser.Email,
}))
    .WithName("GetCurrentUser");

protectedApi.MapRecipeEndpoints();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering",
    "Scorching"
};

protectedApi.MapGet("/weatherforecast", (ICurrentUserService currentUser) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return Results.Ok(new
    {
        userId = currentUser.UserId,
        items = forecast,
    });
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
