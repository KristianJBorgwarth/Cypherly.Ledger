namespace Ledger.API.Requests;

internal sealed record CreateLedgerRequest
{
    public required Guid LedgerId { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKey { get; init; }
    public required byte[] Signature { get; init; }
    public required KeyChangesRequest KeyChanges { get; init; }
}
