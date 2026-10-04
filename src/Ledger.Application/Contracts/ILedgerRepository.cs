using Ledger.Application.Abstractions;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Entities;

public interface ILedgerRepository : IRepository<LedgerStream>
{
    Task<IReadOnlyList<LedgerEvent>> GetEventsAsync(Guid ledgerId, int fromVersion, int limit, CancellationToken ct = default);
}
