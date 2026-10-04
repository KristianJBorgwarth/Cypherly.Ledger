using System.Reflection;
using Ledger.Application.Abstractions;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ledger.Infrastructure.Extensions;

internal static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment, Assembly assembly)
    {
        services.AddDbContext<LedgerDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("LedgerDbConnectionString"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(assembly.FullName);
                    sqlOptions.EnableRetryOnFailure();
                });

            // Parameter values end up in logs, so keep this out of production
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        });

        services.AddRepositories();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
    }
}
