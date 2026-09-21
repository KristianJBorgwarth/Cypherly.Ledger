using Ledger.Domain.Abstractions;

namespace Ledger.Domain.Entities;

public sealed class LedgerWriteKey : Entity
{
    public Guid LedgerId { get; private set; }
    public byte[] PublicKey { get; private set; } = [32];

    private LedgerWriteKey(Guid id) : base(id) { } // For EF Core

    internal LedgerWriteKey(Guid ledgerId, byte[] publicKey) : base(Guid.NewGuid())
    {
        if (publicKey.Length != 32)
            throw new ArgumentException("Public key must be 32 bytes long.", nameof(publicKey));

        LedgerId = ledgerId;
        PublicKey = publicKey;
    }
}
