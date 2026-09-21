using Ledger.Domain.Abstractions;

namespace Ledger.Domain.Entities;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public sealed class LedgerEvent : Entity
{
    public Guid StreamId { get; private set; }
    public int Version { get; private set; }
    public byte[] Payload { get; private set; }
    public byte[] PreviousHash { get; private set; }
    public byte[] WriteKeyPublic { get; private set; }
    public byte[] Signature { get; private set; }

    private LedgerEvent(Guid id) : base(id) { } // For EF Core

    internal LedgerEvent(
        Guid streamId, 
        int version, 
        byte[] payload, 
        byte[] previousHash, 
        byte[] writeKeyPublic, 
        byte[] signature) : base(Guid.NewGuid())
    {
        StreamId = streamId;
        Version = version;
        Payload = payload;
        PreviousHash = previousHash;
        WriteKeyPublic = writeKeyPublic;
        Signature = signature;
    }
}
