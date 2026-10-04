using Ledger.Domain.Aggregates;

namespace Ledger.Application.Dto;

public sealed record LedgerHeadDto
{
    public required int Version { get; init; }
    public required byte[] Hash { get; init; }
    public required bool Archived { get; init; }

    public static LedgerHeadDto MapFrom(LedgerStream ledger) => new()
    {
        Version = ledger.Version,
        Hash = ledger.Hash,
        Archived = ledger.Archived,
    };
}
