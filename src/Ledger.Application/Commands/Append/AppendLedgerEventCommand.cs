using Ledger.Application.Abstractions;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Append;

public sealed record AppendLedgerEventCommand : ICommand
{
    public required Guid LedgerId { get; init; }
    public required int ExpectedVersion { get; init; }
    public required byte[] PreviousHash { get; init; }
    public required byte[] Payload { get; init; }
    public required byte[] WriteKeyPublic { get; init; }
    public required byte[] Signature { get; init; }
    public required KeyChanges KeyChanges { get; init; }
}
