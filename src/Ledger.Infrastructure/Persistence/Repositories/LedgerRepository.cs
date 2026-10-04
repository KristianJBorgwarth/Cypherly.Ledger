using Ledger.Application.Abstractions;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Entities;
using Ledger.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

internal sealed class LedgerRepository(LedgerDbContext ctx) : ILedgerRepository
{
    public async Task CreateAsync(LedgerStream entity, CancellationToken ct = default)
    {
        await ctx.LedgerStream.AddAsync(entity, ct);
    }

    public void Delete(LedgerStream entity)
    {
        ctx.LedgerStream.Remove(entity);
    }

    public async Task<LedgerStream?> GetAsync(ISpecification<LedgerStream> spec, CancellationToken ct = default)
    {
        var query = ctx.LedgerStream.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<LedgerEvent>> GetEventsAsync(Guid ledgerId, int fromVersion, int limit, CancellationToken ct = default)
    {
        return await ctx.LedgerEvent
            .AsNoTracking()
            .Where(e => e.StreamId == ledgerId && e.Version >= fromVersion)
            .OrderBy(e => e.Version)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<LedgerStream?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await ctx.LedgerStream.FirstOrDefaultAsync(l => l.Id == id, ct);
    }
}
