namespace Ledger.API.Requests;

internal sealed record KeyChangesRequest
{
    public required List<byte[]> Added { get; init; }
    public required List<byte[]> InvitesAdded { get; init; }
    public required List<byte[]> Removed { get; init; }
}
