namespace Ledger.Application.Dto;

public sealed record KeyChangesDto
{
    public required List<byte[]> Added { get; init; }
    public required List<byte[]> InvitesAdded { get; init; }
    public required List<byte[]> Removed { get; init; }
}
