namespace Ledger.Application.Dto;

public sealed record LedgerEventsDto
{
    public required int Version { get; init; }
    public required bool Archived { get; init; }
    public required bool HasMore { get; init; }
    public required List<LedgerEventDto> Events { get; init; }
}
