using Microsoft.IdentityModel.Tokens;

namespace Ledger.API.Extensions;

internal static class AuthenticationExtensions
{
    public static void AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Jwt:Issuer"]
            ?? throw new NotImplementedException($"MISSING VALUE IN JWT SETTINGS {configuration["Jwt:Authority"]}");

        var audience = configuration["Jwt:Audience"]
            ?? throw new NotImplementedException($"MISSING VALUE IN JWT SETTINGS {configuration["Jwt:Audience"]}");

        services.AddAuthentication().AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.Authority = authority;
            options.Audience = audience;

            options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
            options.RefreshInterval = TimeSpan.FromMinutes(1);
            options.RefreshOnIssuerKeyNotFound = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authority,
                ValidAudience = audience,
            };
        });

        services.AddAuthorization();
    }
}
