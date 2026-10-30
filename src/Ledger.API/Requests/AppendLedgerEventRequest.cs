namespace Ledger.API.Requests;

internal sealed record AppendLedgerEventRequest
{
    public required int ExpectedVersion { get; init; }
    public required byte[] PreviousHash { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required byte[] Signature { get; init; }
    public required KeyChangesRequest KeyChanges { get; init; }
}
