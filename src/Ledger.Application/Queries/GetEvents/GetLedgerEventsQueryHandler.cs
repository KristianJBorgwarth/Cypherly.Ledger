using Ledger.Application.Abstractions;
using Ledger.Application.Queries.GetEvents;
using Ledger.Domain.Common;
using Ledger.Domain.Entities;

public sealed class GetLedgerEventsQueryHandler : IQueryHandler<GetLedgerEventsQuery, LedgerEvent>
{
    public ValueTask<Result<LedgerEvent>> Handle(GetLedgerEventsQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
