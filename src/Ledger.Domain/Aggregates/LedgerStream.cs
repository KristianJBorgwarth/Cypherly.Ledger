
using Ledger.Domain.Abstractions;
using Ledger.Domain.Common;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;

namespace Ledger.Domain.Aggregates;

public sealed class LedgerStream : AggregateRoot
{
    private const int KeyLength = 32;
    private const int HashLength = 32;
    private const int SignatureLength = 64;

    private readonly List<LedgerWriteKey> _writeKeys = [];
    private readonly List<LedgerEvent> _events = [];

    public int Version { get; private set; }
    public byte[] Hash { get; private set; } = new byte[32];
    public bool Archived { get; private set; }
    public IReadOnlyCollection<LedgerWriteKey> WriteKeys => _writeKeys.AsReadOnly();
    public IReadOnlyCollection<LedgerEvent> Events => _events.AsReadOnly();

    // Private constructor for EF Core
    private LedgerStream(Guid id) : base(id) { }

    public LedgerStream(Guid id, byte[] createWriteKey) : base(id)
    {
        _writeKeys.Add(new LedgerWriteKey(id, createWriteKey, WriteKeyRole.Owner));
    }

    public bool HasWriteKey(byte[] publicKey) => _writeKeys.Any(wk => wk.PublicKey.SequenceEqual(publicKey));

    public Result<LedgerEvent> Append(
            int expectedVersion,
            byte[] previousHash,
            byte[] eventHash,
            byte[] payload,
            byte[] writeKeyPublic,
            byte[] signature,
            KeyChanges keyChanges)
    {
        if (payload.Length == 0
            || previousHash.Length != HashLength
            || eventHash.Length != HashLength
            || writeKeyPublic.Length != KeyLength
            || signature.Length != SignatureLength
            || keyChanges.Added.Concat(keyChanges.InvitesAdded).Concat(keyChanges.Removed).Any(k => k.Length != KeyLength))
            throw new ArgumentException("Malformed append.");

        if (Archived)
            return Result.Fail<LedgerEvent>(Error.NotFound<LedgerStream>(Id.ToString()));

        var signer = _writeKeys.FirstOrDefault(k => k.PublicKey.AsSpan().SequenceEqual(writeKeyPublic));

        if (signer is null)
            return Result.Fail<LedgerEvent>(Error.Forbidden("Write key is not authorized for this ledger."));

        if (signer.Role is WriteKeyRole.Invite && !IsSelfSwap(writeKeyPublic, keyChanges))
            return Result.Fail<LedgerEvent>(Error.Forbidden("An invite key may only register its own write key."));

        if (expectedVersion != Version || !previousHash.AsSpan().SequenceEqual(Hash))
            return Result.Fail<LedgerEvent>(Error.Conflict($"Ledger is at version {Version}, append expected {expectedVersion}."));

        var remaining = _writeKeys.Count(k => k.Role is not WriteKeyRole.Invite && !keyChanges.Removed.Any(r => r.AsSpan().SequenceEqual(k.PublicKey)))
                        + keyChanges.Added.Count(k => !HasWriteKey(k));

        if (remaining == 0)
            return Result.Fail<LedgerEvent>(Error.Validation("Append would leave the ledger without write keys."));

        var evt = new LedgerEvent(Id, Version + 1, payload, previousHash, writeKeyPublic, signature, keyChanges);

        _events.Add(evt);

        foreach (var key in keyChanges.Removed)
            _writeKeys.RemoveAll(k => k.PublicKey.AsSpan().SequenceEqual(key));

        foreach (var key in keyChanges.Added.Where(k => !HasWriteKey(k)))
            _writeKeys.Add(new LedgerWriteKey(Id, key, WriteKeyRole.Writer));

        foreach (var key in keyChanges.InvitesAdded.Where(k => !HasWriteKey(k)))
            _writeKeys.Add(new LedgerWriteKey(Id, key, WriteKeyRole.Invite));

        Version = evt.Version;
        Hash = eventHash;

        return evt;
    }

    public static Result<LedgerStream> Initialize(
        Guid id,
        byte[] creatorWriteKey,
        byte[] payload,
        byte[] eventhash,
        byte[] signature,
        IReadOnlyCollection<byte[]> inviteKeysAdded)
    {
        var ledger = new LedgerStream(id, creatorWriteKey);

        var append = ledger.Append(
            expectedVersion: 0,
            eventHash: eventhash,
            previousHash: new byte[32],
            payload: payload,
            writeKeyPublic: creatorWriteKey,
            signature: signature,
            keyChanges: new KeyChanges([], inviteKeysAdded, []));

        return append.Success ? ledger : Result.Fail<LedgerStream>(append.Error);
    }

    public Result<LedgerEvent> Archive(
        int expectedVersion,
        byte[] previousHash,
        byte[] eventHash,
        byte[] payload,
        byte[] writeKeyPublic,
        byte[] signature)
    {
        var append = Append(
            expectedVersion: expectedVersion,
            previousHash: previousHash,
            eventHash: eventHash,
            payload: payload,
            writeKeyPublic: writeKeyPublic,
            signature: signature,
            keyChanges: KeyChanges.None);

        if (append.Success is false)
            return append;

        Archived = true;

        return append;
    }

    private bool IsSelfSwap(byte[] signer, KeyChanges keyChanges) =>
        keyChanges.InvitesAdded.Count == 0
        && keyChanges.Added.Count == 1
        && !HasWriteKey(keyChanges.Added.First())
        && keyChanges.Removed.Count == 1
        && keyChanges.Removed.First().AsSpan().SequenceEqual(signer);
}
