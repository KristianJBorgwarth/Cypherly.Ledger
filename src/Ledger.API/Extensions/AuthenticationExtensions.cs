using System.Buffers.Text;
using PrivacyPass.AspNetCore.Authentication;
using PrivacyPass.AspNetCore.Extensions;

namespace Ledger.API.Extensions;

internal static class AuthenticationExtensions
{
    public static void AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddPrivateToken(options =>
            {
                options.IssuerName = configuration["PrivateToken:IssuerName"]
                    ?? throw new NotImplementedException("MISSING VALUE IN PRIVATE TOKEN SETTINGS IssuerName");
                options.Origin = configuration["PrivateToken:Origin"]
                    ?? throw new NotImplementedException("MISSING VALUE IN PRIVATE TOKEN SETTINGS Origin");
                options.IssuerPublicKey = Base64Url.DecodeFromChars(configuration["PrivateToken:IssuerPublicKey"] ?? string.Empty);
            });

        services.AddOptions<PrivateTokenOptions>(PrivateTokenDefaults.AuthenticationScheme).ValidateOnStart();

        services.AddAuthorization();
    }
}
