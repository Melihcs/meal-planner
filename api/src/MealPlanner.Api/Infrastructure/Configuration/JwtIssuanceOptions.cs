namespace MealPlanner.Api.Infrastructure.Configuration;

public sealed class JwtIssuanceOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public int ExpiryDays { get; init; } = 7;

    public static JwtIssuanceOptions FromConfiguration(IConfiguration configuration)
    {
        return new JwtIssuanceOptions
        {
            Issuer = configuration["JWT_ISSUER"] ?? string.Empty,
            Audience = configuration["JWT_AUDIENCE"] ?? string.Empty,
            Secret = configuration["JWT_SECRET"] ?? string.Empty,
            ExpiryDays = configuration.GetValue<int?>("JWT_EXPIRY_DAYS") ?? 7,
        };
    }
}
