using Ledger.Test.Integration.Setup.Authentication;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrivacyPass.AspNetCore.Authentication;
using Testcontainers.PostgreSql;

namespace Ledger.Test.Integration.Setup;

public class IntegrationTestFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class where TDbContext : DbContext
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:latest")
        .WithCleanUp(true)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("PrivateToken:IssuerPublicKey", PrivateTokens.IssuerPublicKey);

        builder.ConfigureServices(services =>
        {
            #region Database Extensions

            // EF Core keeps the app's options configuration in IDbContextOptionsConfiguration<T>,
            // so both have to go or the app's connection string is applied on top of the container's
            services.RemoveAll<DbContextOptions<TDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TDbContext>>();

            services.AddDbContext<TDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString(),
                    b => b.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));
            });

            #endregion

            #region Authentication

            // The real scheme needs an identity server, so tests authenticate any request that carries a Bearer header.
            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.AddAuthorizationBuilder()
                .AddPolicy("create-stream", policy => policy
                    .AddAuthenticationSchemes(TestAuthHandler.Scheme, PrivateTokenDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser());

            #endregion
        });
    }

    public virtual async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await db.Database.MigrateAsync();
    }

    public new virtual async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
