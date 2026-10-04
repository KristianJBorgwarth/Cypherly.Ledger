using Ledger.Application.Commands.Append;
using Ledger.Application.Commands.Archive;
using Ledger.Application.Commands.Create;
using Ledger.Domain.Aggregates;
using NSec.Cryptography;

namespace Ledger.Test.Integration.Setup.Helpers;

public static class LedgerCommands
{
    public static CreateLedgerCommand Create(Guid ledgerId, byte[]? payload = null) => new()
    {
        LedgerId = ledgerId,
        Payload = payload ?? [1, 2, 3],
        WriteKey = [],
        Signature = [],
    };

    public static AppendLedgerEventCommand Append(LedgerStream ledger, byte[]? payload = null) => new()
    {
        LedgerId = ledger.Id,
        ExpectedVersion = ledger.Version,
        PreviousHash = ledger.Hash,
        Payload = payload ?? [4, 5, 6],
        WriteKeyPublic = [],
        Signature = [],
        KeysAdded = [],
        KeysRemoved = [],
    };

    public static ArchiveLedgerCommand Archive(LedgerStream ledger, byte[]? payload = null) => new()
    {
        LedgerId = ledger.Id,
        ExpectedVersion = ledger.Version,
        PreviousHash = ledger.Hash,
        Payload = payload ?? [0xAA],
        WriteKeyPublic = [],
        Signature = [],
    };

    public static ArchiveLedgerCommand SignedBy(this ArchiveLedgerCommand cmd, Key key) => cmd with
    {
        WriteKeyPublic = LedgerSigner.PublicKeyOf(key),
        Signature = LedgerSigner.Sign(key, cmd.LedgerId, cmd.ExpectedVersion + 1, cmd.PreviousHash, cmd.Payload),
    };

    public static CreateLedgerCommand SignedBy(this CreateLedgerCommand cmd, Key key) => cmd with
    {
        WriteKey = LedgerSigner.PublicKeyOf(key),
        Signature = LedgerSigner.Sign(key, cmd.LedgerId, LedgerFactory.GenesisVersion, LedgerFactory.GenesisHash, cmd.Payload),
    };

    public static AppendLedgerEventCommand SignedBy(this AppendLedgerEventCommand cmd, Key key) => cmd with
    {
        WriteKeyPublic = LedgerSigner.PublicKeyOf(key),
        Signature = LedgerSigner.Sign(key, cmd.LedgerId, cmd.ExpectedVersion + 1, cmd.PreviousHash, cmd.Payload, cmd.KeysAdded, cmd.KeysRemoved),
    };
}
