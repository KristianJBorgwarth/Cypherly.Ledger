using Ledger.Application.Abstractions;
using Ledger.Application.Dto;

namespace Ledger.Application.Queries.GetHead;

public sealed record GetLedgerHeadQuery : IQuery<LedgerHeadDto>
{
    public required Guid LedgerId { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required long Timestamp { get; init; }
    public required byte[] Signature { get; init; }
}
