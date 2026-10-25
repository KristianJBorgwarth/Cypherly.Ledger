using System.Buffers.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PrivacyPass.AspNetCore.Authentication;
using PrivacyPass.AspNetCore.Extensions;

namespace Ledger.API.Extensions;

internal static class AuthenticationExtensions
{
    public const string CreateStreamPolicy = "create-stream";

    public static void AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Jwt:Issuer"]
            ?? throw new NotImplementedException($"MISSING VALUE IN JWT SETTINGS {configuration["Jwt:Authority"]}");

        var audience = configuration["Jwt:Audience"]
            ?? throw new NotImplementedException($"MISSING VALUE IN JWT SETTINGS {configuration["Jwt:Audience"]}");

        services.AddAuthentication()
            .AddJwtBearer(options =>
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
        })
            .AddPrivateToken(options =>
            {
                options.IssuerName = configuration["PrivateToken:IssuerName"]
                    ?? throw new NotImplementedException("MISSING VALUE IN PRIVATE TOKEN SETTINGS IssuerName");
                options.Origin = configuration["PrivateToken:Origin"]
                    ?? throw new NotImplementedException("MISSING VALUE IN PRIVATE TOKEN SETTINGS Origin");
                options.IssuerPublicKey = Base64Url.DecodeFromChars(configuration["PrivateToken:IssuerPublicKey"] ?? string.Empty);
            });

        services.AddOptions<PrivateTokenOptions>(PrivateTokenDefaults.AuthenticationScheme).ValidateOnStart();

        services.AddAuthorizationBuilder()
            .AddPolicy(CreateStreamPolicy, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, PrivateTokenDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
    }
}
