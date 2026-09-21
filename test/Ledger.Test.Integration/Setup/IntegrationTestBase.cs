using AutoFixture;
using Ledger.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

#pragma warning disable CA1816
namespace Ledger.Test.Integration.Setup;

[Collection("LedgerApplication")]
public class IntegrationTestBase : IDisposable
{
    private readonly IServiceScope _scope;
    protected readonly LedgerDbContext Db;
    protected readonly HttpClient Client;
    protected readonly Fixture Fixture = new();

    public IntegrationTestBase(IntegrationTestFactory<Program, LedgerDbContext> factory)
    {
        _scope = factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        Client = factory.CreateClient();
    }

    public void Dispose()
    {
        Db.OutboxMessage.ExecuteDelete();
        Client.Dispose();
        _scope.Dispose();
    }
}
