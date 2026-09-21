using Ledger.Infrastructure.Persistence.Outbox;

namespace Ledger.Infrastructure.Persistence.Repositories;

internal interface IOutboxRepository
{
    Task<OutboxMessage[]> GetUnprocessedAsync(int batchSize);
    Task MarkAsProcessedAsync(OutboxMessage message);
}