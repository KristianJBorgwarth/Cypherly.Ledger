using Ledger.Application.Abstractions;

namespace Ledger.Application.Commands.Archive;

public sealed class ArchiveLedgerCommand : ICommand
{
    public required Guid LedgerId { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
}
