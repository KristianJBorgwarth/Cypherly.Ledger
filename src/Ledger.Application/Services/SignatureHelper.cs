using System.Buffers.Binary;
using System.Security.Cryptography;
using Ledger.Application.Interfaces;
using NSec.Cryptography;

namespace Ledger.Application.Services;

internal sealed class SignatureHelper : ISignatureHelper
{
    public byte[] GenerateHash(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        IReadOnlyCollection<byte[]> keysAdded,
        IReadOnlyCollection<byte[]> keysRemoved)
    {
        var signingInput = SigningInput(ledgerId, version, previousHash, payload, keysAdded, keysRemoved);
        return SHA256.HashData(signingInput);
    }

    public bool IsValidSignature(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        byte[] writeKeyPublic,
        byte[] signature,
        IReadOnlyCollection<byte[]> keysAdded,
        IReadOnlyCollection<byte[]> keysRemoved)
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        if (!PublicKey.TryImport(algorithm, writeKeyPublic, KeyBlobFormat.RawPublicKey, out var key))
            return false;

        var signingInput = SigningInput(ledgerId, version, previousHash, payload, keysAdded, keysRemoved);

        return algorithm.Verify(key!, signingInput, signature);
    }

    /// <summary>
    /// Generates the signing input for the given ledger event data.
    /// </summary>
    private static byte[] SigningInput(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        IReadOnlyCollection<byte[]> keysAdded,
        IReadOnlyCollection<byte[]> keysRemoved)
    {
        using var input = new MemoryStream();
        input.Write("ledger-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(BigEndian(version));
        input.Write(previousHash);
        WriteKeys(input, keysAdded);
        WriteKeys(input, keysRemoved);
        input.Write(payload);

        return input.ToArray();
    }

    // Count-prefixed and sorted: the count keeps the lists unambiguous against each other and the payload,
    // the sort makes the signature independent of the order the client happened to send them in.
    private static void WriteKeys(Stream input, IReadOnlyCollection<byte[]> keys)
    {
        input.Write(BigEndian(keys.Count));

        foreach (var key in keys.OrderBy(Convert.ToHexString, StringComparer.Ordinal))
            input.Write(key);
    }

    private static byte[] BigEndian(int value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        return bytes;
    }
}
