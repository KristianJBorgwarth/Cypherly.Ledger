using Ledger.Domain.Common;

namespace Ledger.Application.Interfaces;

public interface ISignatureHelper
{
    /// <summary>
    /// Generates a hash for the given ledger event data.
    /// </summary>
    public byte[] GenerateHash(
        Guid ledgerId, 
        int version, 
        byte[] previousHash, 
        byte[] payload,
        KeyChanges keyChanges);

    /// <summary>
    /// Validates the signature for the given ledger event data.
    /// </summary>
    public bool IsValidSignature(
        Guid ledgerId, 
        int version, 
        byte[] previousHash, 
        byte[] payload, 
        byte[] writeKeyPublic, 
        byte[] signature,
        KeyChanges keyChanges);

    public bool IsValidReadSignature(Guid ledgerId, long timestamp, byte[] writeKeyPublic, byte[] signature);
}
