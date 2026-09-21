using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ledger.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        Assembly assembly)
    {
        services.AddPersistence(configuration, environment, assembly);
    }
}
