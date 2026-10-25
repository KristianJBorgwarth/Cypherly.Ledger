using Ledger.Domain.Aggregates;
using Ledger.Domain.Entities;
using Ledger.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using PrivacyPass.EntityFrameworkCore.PostgreSQL.Extensions;

namespace Ledger.Infrastructure.Persistence.Context;

public class LedgerDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessage { get; init; }
    public DbSet<LedgerStream> LedgerStream { get; init; }
    public DbSet<LedgerEvent> LedgerEvent { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
        modelBuilder.ApplySpentTokens();
        base.OnModelCreating(modelBuilder);
    }
}
