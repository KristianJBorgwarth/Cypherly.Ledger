using System.Buffers.Binary;
using System.Security.Cryptography;
using Ledger.Application.Common;
using Ledger.Application.Interfaces;
using Ledger.Domain.Common;
using NSec.Cryptography;

namespace Ledger.Application.Services;

internal sealed class SignatureHelper : ISignatureHelper
{
    public byte[] GenerateHash(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        KeyChanges keyChanges)
    {
        var signingInput = SigningInput(ledgerId, version, previousHash, payload, keyChanges);
        return SHA256.HashData(signingInput);
    }

    public bool IsValidSignature(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        byte[] writeKeyPublic,
        byte[] signature,
        KeyChanges keyChanges)
    {
        var algorithm = SignatureAlgorithm.Ed25519;

        if (!PublicKey.TryImport(algorithm, writeKeyPublic, KeyBlobFormat.RawPublicKey, out var key))
            return false;

        var signingInput = SigningInput(ledgerId, version, previousHash, payload, keyChanges);

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
        KeyChanges keyChanges)
    {
        using var input = new MemoryStream();
        input.Write("ledger-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(BigEndian(version));
        input.Write(previousHash);
        WriteKeys(input, keyChanges.Added);
        WriteKeys(input, keyChanges.InvitesAdded);
        WriteKeys(input, keyChanges.Removed);
        input.Write(payload);

        return input.ToArray();
    }

    // Count-prefixed and sorted: the count keeps the lists unambiguous against each other and the payload,
    // the sort makes the signature independent of the order the client happened to send them in.
    public bool IsValidReadSignature(Guid ledgerId, long timestamp, byte[] writeKeyPublic, byte[] signature)
    {
        if (Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp) > LedgerQueryLimits.ReadSignatureMaxAgeSeconds)
            return false;

        var algorithm = SignatureAlgorithm.Ed25519;

        if (!PublicKey.TryImport(algorithm, writeKeyPublic, KeyBlobFormat.RawPublicKey, out var key))
            return false;

        return algorithm.Verify(key!, ReadSigningInput(ledgerId, timestamp), signature);
    }

    private static byte[] ReadSigningInput(Guid ledgerId, long timestamp)
    {
        var timestampBytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(timestampBytes, timestamp);

        using var input = new MemoryStream();
        input.Write("ledger-read-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(timestampBytes);

        return input.ToArray();
    }

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
