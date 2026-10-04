using Ledger.Application.Abstractions;
using Ledger.Application.Commands.Append;
using Ledger.Domain.Common;

public sealed class AppendLedgerEventCommandHandler : ICommandHandler<AppendLedgerEventCommand>
{
    public ValueTask<Result> Handle(AppendLedgerEventCommand cmd, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
