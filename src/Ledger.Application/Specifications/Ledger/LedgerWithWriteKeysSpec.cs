using Ledger.Application.Abstractions;
using Ledger.Domain.Aggregates;

internal sealed class LedgerWithWriteKeysSpec : Specification<LedgerStream>
{
    public LedgerWithWriteKeysSpec(Guid id) : base(ledger => ledger.Id == id)
    {
        AddIncludes($"{nameof(LedgerStream.WriteKeys)}");
    }
}
