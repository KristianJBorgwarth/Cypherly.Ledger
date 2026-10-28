using System.Buffers.Binary;
using System.Security.Cryptography;
using NSec.Cryptography;

namespace Ledger.Test.Integration.Setup.Helpers;

/// <summary>
/// Mirrors the signing input a client builds. Deliberately a second implementation: if the server's
/// layout changes, these tests fail instead of silently agreeing with it.
/// </summary>
public static class LedgerSigner
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    public static Key NewKey() =>
        Key.Create(Algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

    public static byte[] PublicKeyOf(Key key) => key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

    public static byte[] Sign(
        Key key,
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        IReadOnlyCollection<byte[]>? keysAdded = null,
        IReadOnlyCollection<byte[]>? keysRemoved = null,
        IReadOnlyCollection<byte[]>? inviteKeysAdded = null) =>
        Algorithm.Sign(key, SigningInput(ledgerId, version, previousHash, payload, keysAdded ?? [], inviteKeysAdded ?? [], keysRemoved ?? []));

    public static byte[] Hash(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        IReadOnlyCollection<byte[]>? keysAdded = null,
        IReadOnlyCollection<byte[]>? keysRemoved = null,
        IReadOnlyCollection<byte[]>? inviteKeysAdded = null) =>
        SHA256.HashData(SigningInput(ledgerId, version, previousHash, payload, keysAdded ?? [], inviteKeysAdded ?? [], keysRemoved ?? []));

    public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public static byte[] SignRead(Key key, Guid ledgerId, long timestamp)
    {
        var timestampBytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(timestampBytes, timestamp);

        using var input = new MemoryStream();
        input.Write("ledger-read-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(timestampBytes);

        return Algorithm.Sign(key, input.ToArray());
    }

    private static byte[] SigningInput(
        Guid ledgerId,
        int version,
        byte[] previousHash,
        byte[] payload,
        IReadOnlyCollection<byte[]> keysAdded,
        IReadOnlyCollection<byte[]> inviteKeysAdded,
        IReadOnlyCollection<byte[]> keysRemoved)
    {
        using var input = new MemoryStream();
        input.Write("ledger-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(BigEndian(version));
        input.Write(previousHash);
        WriteKeys(input, keysAdded);
        WriteKeys(input, inviteKeysAdded);
        WriteKeys(input, keysRemoved);
        input.Write(payload);

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
