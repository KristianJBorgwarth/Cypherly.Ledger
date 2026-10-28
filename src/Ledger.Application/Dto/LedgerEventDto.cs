using Ledger.Domain.Entities;

namespace Ledger.Application.Dto;

public sealed record LedgerEventDto
{
    public required int Version { get; init; }
    public required byte[] PreviousHash { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required byte[] Signature { get; init; }
    public required KeyChangesDto KeyChanges { get; init; }

    public static LedgerEventDto MapFrom(LedgerEvent evt) => new()
    {
        Version = evt.Version,
        PreviousHash = evt.PreviousHash,
        Payload = evt.Payload,
        WriteKeyPublic = evt.WriteKeyPublic,
        Signature = evt.Signature,
        KeyChanges = new KeyChangesDto
        {
            Added = evt.KeysAdded,
            InvitesAdded = evt.InviteKeysAdded,
            Removed = evt.KeysRemoved,
        },
    };
}
