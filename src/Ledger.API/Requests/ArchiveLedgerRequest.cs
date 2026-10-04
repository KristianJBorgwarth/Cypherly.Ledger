namespace Ledger.API.Requests;

internal sealed record ArchiveLedgerRequest
{
    public required byte[] WriteKeyPublic { get; init; }
}
