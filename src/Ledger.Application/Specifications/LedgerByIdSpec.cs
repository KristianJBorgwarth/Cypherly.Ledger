using Ledger.Application.Abstractions;
using Ledger.Domain.Aggregates;

internal sealed class LedgerByIdSpec(Guid id) : Specification<LedgerStream>(c => c.Id == id);
