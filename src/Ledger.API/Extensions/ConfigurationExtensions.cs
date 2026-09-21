using System.Reflection;

namespace Ledger.API.Extensions;

internal static class ConfigurationExtensions
{
    public static IConfiguration SetupConfiguration(this WebApplicationBuilder builder)
    {
        var env = builder.Environment;

        var configuration = builder.Configuration;
        configuration.AddJsonFile("appsettings.json", false, true).AddEnvironmentVariables();

        if (env.IsDevelopment())
        {
            configuration.AddJsonFile($"appsettings.{Environments.Development}.json", true, true);
            configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        }

        return configuration;
    }
}
