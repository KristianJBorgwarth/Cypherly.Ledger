
using System.Buffers.Binary;
using System.Security.Cryptography;
using Ledger.Application.Interfaces;
using NSec.Cryptography;

internal sealed class SignatureHelper : ISignatureHelper
{
    public byte[] GenerateHash(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload)
    {
        var signingInput = SigningInput(ledgerId, version, previousHash, payload);
        return SHA256.HashData(signingInput);
    }

    public bool IsValidSignature(
        Guid ledgerId, 
        int version, 
        byte[] previousHash, 
        byte[] payload, 
        byte[] writeKeyPublic, 
        byte[] signature)
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        if (!PublicKey.TryImport(algorithm, writeKeyPublic, KeyBlobFormat.RawPublicKey, out var key))
            return false;

        var signingInput = SigningInput(ledgerId, version, previousHash, payload);

        return algorithm.Verify(key!, signingInput, signature);
    }

    /// <summary>
    /// Generates the signing input for the given ledger event data.
    /// </summary>
    private static byte[] SigningInput(
        Guid ledgerId, 
        int version, 
        byte[] previousHash, 
        byte[] payload)
    {
        var versionBytes = BitConverter.GetBytes(version);
        BinaryPrimitives.WriteInt32BigEndian(versionBytes, version);

        using var input = new MemoryStream();
        input.Write("ledger-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(versionBytes);
        input.Write(previousHash);
        input.Write(payload);

        return input.ToArray();
    }
}
