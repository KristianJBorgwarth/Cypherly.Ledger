using Ledger.Application.Abstractions;
using Ledger.Domain.Aggregates;
using Ledger.Infrastructure.Persistence.Context;

internal sealed class LedgerRepository(LedgerDbContext ctx) : ILedgerRepository
{
    public Task CreateAsync(LedgerStream entity, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void Delete(LedgerStream entity)
    {
        throw new NotImplementedException();
    }

    public Task<LedgerStream?> GetAsync(ISpecification<LedgerStream> spec, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<LedgerStream> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
