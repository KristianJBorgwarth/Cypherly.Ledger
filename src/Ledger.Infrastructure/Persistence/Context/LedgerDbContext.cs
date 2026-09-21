using Ledger.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Persistence.Context;

public class LedgerDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessage { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
