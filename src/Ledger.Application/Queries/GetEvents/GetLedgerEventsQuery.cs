using Ledger.Application.Abstractions;
using Ledger.Application.Dto;

namespace Ledger.Application.Queries.GetEvents;

public sealed record GetLedgerEventsQuery : IQuery<LedgerEventsDto>
{
    public required Guid LedgerId { get; init; }
    public required int FromVersion { get; init; }
    public required int Limit { get; init; }
}
