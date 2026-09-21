
using Ledger.Domain.Abstractions;
using Ledger.Domain.Common;
using Ledger.Domain.Entities;

namespace Ledger.Domain.Aggregates;

public sealed class LedgerStream : AggregateRoot
{
    private const int KeyLength = 32;
    private const int HashLength = 32;
    private const int SignatureLength = 64;

    private readonly List<LedgerWriteKey> _writeKeys = [];
    private readonly List<LedgerEvent> _events = [];

    public int HeadVersion { get; private set; }
    public byte[] HeadHash { get; private set; } = new byte[32];
    public bool Archived { get; private set; }
    public IReadOnlyCollection<LedgerWriteKey> WriteKeys => _writeKeys.AsReadOnly();
    public IReadOnlyCollection<LedgerEvent> Events => _events.AsReadOnly();

    // Private constructor for EF Core
    private LedgerStream(Guid id) : base(id) { }

    public LedgerStream(Guid id, byte[] createWriteKey) : base(id)
    {
        _writeKeys.Add(new LedgerWriteKey(id, createWriteKey));
    }

    public bool HasWriteKey(byte[] publicKey) => _writeKeys.Any(wk => wk.PublicKey.SequenceEqual(publicKey));

    public Result<LedgerEvent> Append(
            int expectedVersion,
            byte[] previousHash,
            byte[] eventHash,
            byte[] payload,
            byte[] writeKeyPublic,
            byte[] signature,
            IReadOnlyCollection<byte[]> keysAdded,
            IReadOnlyCollection<byte[]> keysRemoved)
    {
        if (payload.Length == 0
            || previousHash.Length != HashLength
            || eventHash.Length != HashLength
            || writeKeyPublic.Length != KeyLength
            || signature.Length != SignatureLength
            || keysAdded.Concat(keysRemoved).Any(k => k.Length != KeyLength))
            throw new ArgumentException("Malformed append.");

        if (Archived)
            return Result.Fail<LedgerEvent>(Error.NotFound<LedgerStream>(Id.ToString()));

        if (!HasWriteKey(writeKeyPublic))
            return Result.Fail<LedgerEvent>(Error.Forbidden("Write key is not authorized for this ledger."));

        if (expectedVersion != HeadVersion || !previousHash.AsSpan().SequenceEqual(HeadHash))
            return Result.Fail<LedgerEvent>(Error.Conflict($"Ledger is at version {HeadVersion}, append expected {expectedVersion}."));

        var remaining = _writeKeys.Count(k => !keysRemoved.Any(r => r.AsSpan().SequenceEqual(k.PublicKey))) + keysAdded.Count(k => !HasWriteKey(k));

        if (remaining == 0)
            return Result.Fail<LedgerEvent>(Error.Validation("Append would leave the ledger without write keys."));

        var evt = new LedgerEvent(Id, HeadVersion + 1, payload, previousHash, writeKeyPublic, signature);

        _events.Add(evt);

        foreach (var key in keysRemoved)
            _writeKeys.RemoveAll(k => k.PublicKey.AsSpan().SequenceEqual(key));

        foreach (var key in keysAdded.Where(k => !HasWriteKey(k)))
            _writeKeys.Add(new LedgerWriteKey(Id, key));

        HeadVersion = evt.Version;
        HeadHash = eventHash;

        return evt;
    }

    public Result Archive(byte[] writeKeyPublic)
    {
        if (Archived)
            return Result.Fail(Error.NotFound<LedgerStream>(Id.ToString()));

        if (!HasWriteKey(writeKeyPublic))
            return Result.Fail(Error.Forbidden("Write key is not authorized for this ledger."));

        Archived = true;
        return Result.Ok();
    }
}
