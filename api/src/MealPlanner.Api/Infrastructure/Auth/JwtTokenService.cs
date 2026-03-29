namespace MealPlanner.Api.Infrastructure.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MealPlanner.Api.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;

public sealed class JwtTokenService
{
    private readonly JwtIssuanceOptions options;

    public JwtTokenService(IConfiguration configuration)
    {
        options = JwtIssuanceOptions.FromConfiguration(configuration);
    }

    public string CreateToken(
        string userId,
        string email,
        string displayName,
        string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT_ISSUER must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT_AUDIENCE must be configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Secret) || options.Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT_SECRET must be configured and be at least 32 characters long.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("user_id", userId),
            new(JwtRegisteredClaimNames.Email, email),
            new("email", email),
            new(JwtRegisteredClaimNames.Name, displayName),
            new("name", displayName),
        };

        if (!string.IsNullOrWhiteSpace(pictureUrl))
        {
            claims.Add(new Claim("picture", pictureUrl));
        }

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddDays(Math.Max(options.ExpiryDays, 1)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
