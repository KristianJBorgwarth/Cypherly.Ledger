using Ledger.Application.Abstractions;
using Ledger.Application.Dto;

namespace Ledger.Application.Queries.GetEvents;

public sealed record GetLedgerEventsQuery : IQuery<LedgerEventsDto>
{
    public required Guid LedgerId { get; init; }
    public required int FromVersion { get; init; }
    public required int Limit { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required long Timestamp { get; init; }
    public required byte[] Signature { get; init; }
}
