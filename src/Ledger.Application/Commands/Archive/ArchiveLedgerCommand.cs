using Ledger.Application.Abstractions;

namespace Ledger.Application.Commands.Archive;

public sealed record ArchiveLedgerCommand : ICommand
{
    public required Guid LedgerId { get; init; }
    public required int ExpectedVersion { get; init; }
    public required byte[] PreviousHash { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required byte[] Signature { get; init; }
}
