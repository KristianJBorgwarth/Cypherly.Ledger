using Ledger.Application.Abstractions;

namespace Ledger.Application.Commands.Create;

public sealed record CreateLedgerCommand : ICommand
{
    public required Guid LedgerId { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKey { get; init; }
    public required byte[] Signature { get; init; }
    public required List<byte[]> KeysAdded { get; init; }
}
