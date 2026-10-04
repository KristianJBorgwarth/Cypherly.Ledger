using Ledger.Application.Abstractions;

namespace Ledger.Application.Commands.Append;

public sealed record AppendLedgerEventCommand : ICommand
{
    public required Guid LedgerId { get; init; }
    public required int ExpectedVersion { get; init; }
    public required byte[] PreviousHash { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required byte[] Signature { get; init; }
    public required List<byte[]> KeysAdded { get; init; }
    public required List<byte[]> KeysRemoved { get; init; }
}
