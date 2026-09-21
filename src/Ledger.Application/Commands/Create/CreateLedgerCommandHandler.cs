using Ledger.Application.Abstractions;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Create;

public sealed class CreateLedgerCommandHandler(
    ILedgerRepository ledgerRepository) 
    : ICommandHandler<CreateLedgerCommand>
{
    public async ValueTask<Result> Handle(CreateLedgerCommand cmd, CancellationToken ct)
    {

    }
}
