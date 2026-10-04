using System.Buffers.Binary;
using System.Security.Cryptography;
using FluentAssertions;
using Ledger.Application.Services;
using NSec.Cryptography;

namespace Ledger.Test.Unit.ServiceTest;

public class SignatureHelperTest
{
    private static readonly SignatureAlgorithm Ed25519 = SignatureAlgorithm.Ed25519;

    private readonly SignatureHelper _sut = new();
    private readonly Guid _ledgerId = Guid.NewGuid();
    private readonly byte[] _previousHash = Enumerable.Repeat((byte)0x07, 32).ToArray();
    private readonly byte[] _payload = [1, 2, 3];

    private static Key NewKey() =>
        Key.Create(Ed25519, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

    private static byte[] Public(Key key) => key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

    private static byte[] Bytes(byte value, int length) => [.. Enumerable.Repeat(value, length)];

    private byte[] Sign(Key key, byte[]? payload = null, int version = 1, byte[][]? added = null, byte[][]? removed = null) =>
        Ed25519.Sign(key, ExpectedSigningInput(_ledgerId, version, _previousHash, payload ?? _payload, added ?? [], removed ?? []));

    // Built independently of the implementation: if the layout changes, these tests fail instead of following along.
    private static byte[] ExpectedSigningInput(
        Guid ledgerId, int version, byte[] previousHash, byte[] payload, byte[][] added, byte[][] removed)
    {
        var versionBytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(versionBytes, version);

        using var input = new MemoryStream();
        input.Write("ledger-v1"u8);
        input.Write(ledgerId.ToByteArray(bigEndian: true));
        input.Write(versionBytes);
        input.Write(previousHash);
        WriteKeys(input, added);
        WriteKeys(input, removed);
        input.Write(payload);

        return input.ToArray();
    }

    private static void WriteKeys(Stream input, byte[][] keys)
    {
        var count = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(count, keys.Length);
        input.Write(count);

        foreach (var key in keys.OrderBy(Convert.ToHexString, StringComparer.Ordinal))
            input.Write(key);
    }

    [Fact]
    public void GenerateHash_ShouldMatchProtocolLayout()
    {
        var expected = SHA256.HashData(ExpectedSigningInput(_ledgerId, 1, _previousHash, _payload, [], []));

        var hash = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], []);

        hash.Should().HaveCount(32);
        hash.Should().Equal(expected);
    }

    [Fact]
    public void GenerateHash_ShouldBeDeterministic()
    {
        var first = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], []);
        var second = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], []);

        first.Should().Equal(second);
    }

    [Fact]
    public void GenerateHash_ShouldDifferPerLedger()
    {
        var first = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], []);
        var second = _sut.GenerateHash(Guid.NewGuid(), 1, _previousHash, _payload, [], []);

        first.Should().NotEqual(second);
    }

    [Fact]
    public void GenerateHash_ShouldDifferPerVersion()
    {
        var first = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], []);
        var second = _sut.GenerateHash(_ledgerId, 2, _previousHash, _payload, [], []);

        first.Should().NotEqual(second);
    }

    [Fact]
    public void GenerateHash_ShouldDifferPerPreviousHash()
    {
        var first = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], []);
        var second = _sut.GenerateHash(_ledgerId, 1, Bytes(0x08, 32), _payload, [], []);

        first.Should().NotEqual(second);
    }

    [Fact]
    public void GenerateHash_ShouldIgnoreKeyOrder()
    {
        byte[] a = Bytes(0x01, 32);
        byte[] b = Bytes(0x02, 32);

        var first = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [a, b], []);
        var second = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [b, a], []);

        first.Should().Equal(second);
    }

    [Fact]
    public void GenerateHash_ShouldDistinguishAddedFromRemoved()
    {
        var key = Bytes(0x01, 32);

        var added = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [key], []);
        var removed = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [], [key]);

        added.Should().NotEqual(removed);
    }

    [Fact]
    public void GenerateHash_ShouldDistinguishKeyFromPayload()
    {
        var key = Bytes(0x01, 32);

        var asKey = _sut.GenerateHash(_ledgerId, 1, _previousHash, _payload, [key], []);
        var asPayload = _sut.GenerateHash(_ledgerId, 1, _previousHash, [.. key, .. _payload], [], []);

        asKey.Should().NotEqual(asPayload);
    }

    [Fact]
    public void IsValidSignature_ShouldReturnTrue_WhenSignedByThatKey()
    {
        var key = NewKey();

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, _payload, Public(key), Sign(key), [], []);

        valid.Should().BeTrue();
    }

    [Fact]
    public void IsValidSignature_ShouldReturnTrue_WhenKeysAreListedInAnotherOrder()
    {
        var key = NewKey();
        byte[] a = Bytes(0x01, 32);
        byte[] b = Bytes(0x02, 32);

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, _payload, Public(key),
            Sign(key, added: [a, b]), [b, a], []);

        valid.Should().BeTrue();
    }

    [Fact]
    public void IsValidSignature_ShouldReturnFalse_WhenSignedByAnotherKey()
    {
        var key = NewKey();
        var impostor = NewKey();

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, _payload, Public(key), Sign(impostor), [], []);

        valid.Should().BeFalse();
    }

    [Fact]
    public void IsValidSignature_ShouldReturnFalse_WhenPayloadChanged()
    {
        var key = NewKey();

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, [9, 9, 9], Public(key), Sign(key), [], []);

        valid.Should().BeFalse();
    }

    [Fact]
    public void IsValidSignature_ShouldReturnFalse_WhenVersionChanged()
    {
        var key = NewKey();

        var valid = _sut.IsValidSignature(_ledgerId, 2, _previousHash, _payload, Public(key), Sign(key), [], []);

        valid.Should().BeFalse();
    }

    [Fact]
    public void IsValidSignature_ShouldReturnFalse_WhenKeyMutationChanged()
    {
        var key = NewKey();
        var signedFor = Bytes(0x01, 32);
        var swapped = Bytes(0x02, 32);

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, _payload, Public(key),
            Sign(key, added: [signedFor]), [swapped], []);

        valid.Should().BeFalse();
    }

    [Theory]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(0)]
    public void IsValidSignature_ShouldReturnFalse_WhenPublicKeyIsMalformed(int length)
    {
        var key = NewKey();

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, _payload, Bytes(0xFF, length), Sign(key), [], []);

        valid.Should().BeFalse();
    }

    [Theory]
    [InlineData(63)]
    [InlineData(65)]
    [InlineData(0)]
    public void IsValidSignature_ShouldReturnFalse_WhenSignatureIsMalformed(int length)
    {
        var key = NewKey();

        var valid = _sut.IsValidSignature(_ledgerId, 1, _previousHash, _payload, Public(key), Bytes(0xFF, length), [], []);

        valid.Should().BeFalse();
    }
}
