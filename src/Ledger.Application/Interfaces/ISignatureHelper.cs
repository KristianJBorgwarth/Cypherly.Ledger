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
        IReadOnlyCollection<byte[]> keysAdded,
        IReadOnlyCollection<byte[]> inviteKeysAdded,
        IReadOnlyCollection<byte[]> keysRemoved);

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
        IReadOnlyCollection<byte[]> keysAdded,
        IReadOnlyCollection<byte[]> inviteKeysAdded,
        IReadOnlyCollection<byte[]> keysRemoved);

    public bool IsValidReadSignature(Guid ledgerId, long timestamp, byte[] writeKeyPublic, byte[] signature);
}
