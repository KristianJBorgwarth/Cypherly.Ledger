using Ledger.Application.Abstractions;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Create;

public sealed record CreateLedgerCommand : ICommand
{
    public required Guid LedgerId { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKey { get; init; }
    public required byte[] Signature { get; init; }
    public required KeyChanges KeyChanges { get; init; }
}
