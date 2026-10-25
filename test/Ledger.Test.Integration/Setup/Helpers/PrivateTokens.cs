using System.Buffers.Binary;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using PrivacyPass.Keys;

namespace Ledger.Test.Integration.Setup.Helpers;

public static class PrivateTokens
{
    private const ushort TokenType = 0x0002;
    private const string IssuerName = "identity.cypherly";

    private static readonly RSA Issuer = RSA.Create(2048);

    public static string IssuerPublicKey => Base64Url.EncodeToString(TokenKeyId.EncodePublicKey(Issuer));

    public static string Header(string origin = "ledger", RSA? issuer = null) =>
        $"PrivateToken token={Base64Url.EncodeToString(Mint(origin, issuer ?? Issuer))}";

    private static byte[] Mint(string origin, RSA issuer)
    {
        using var input = new MemoryStream();
        input.Write(BigEndian(TokenType));
        input.Write(RandomNumberGenerator.GetBytes(32));
        input.Write(SHA256.HashData(Challenge(origin)));
        input.Write(SHA256.HashData(TokenKeyId.EncodePublicKey(issuer)));

        var tokenInput = input.ToArray();
        var authenticator = issuer.SignData(tokenInput, HashAlgorithmName.SHA384, RSASignaturePadding.Pss);

        return [.. tokenInput, .. authenticator];
    }

    private static byte[] Challenge(string origin)
    {
        var issuerName = Encoding.UTF8.GetBytes(IssuerName);
        var originInfo = Encoding.UTF8.GetBytes(origin);

        using var challenge = new MemoryStream();
        challenge.Write(BigEndian(TokenType));
        challenge.Write(BigEndian((ushort)issuerName.Length));
        challenge.Write(issuerName);
        challenge.WriteByte(0);
        challenge.Write(BigEndian((ushort)originInfo.Length));
        challenge.Write(originInfo);
        return challenge.ToArray();
    }

    private static byte[] BigEndian(ushort value)
    {
        var bytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        return bytes;
    }
}
