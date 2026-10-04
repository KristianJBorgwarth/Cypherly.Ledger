using Ledger.Application.Abstractions;
using Ledger.Domain.Entities;

namespace Ledger.Application.Queries.GetEvents;

public sealed record GetLedgerEventsQuery : IQuery<LedgerEvent>
{

}
