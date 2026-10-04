using Ledger.Application.Abstractions;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Archive;

public sealed class ArchiveLedgerCommandHandler(
    ILedgerRepository ledgerRepository,
    IUnitOfWork unitOfWork) 
    : ICommandHandler<ArchiveLedgerCommand>
{
    public async ValueTask<Result> Handle(ArchiveLedgerCommand cmd, CancellationToken ct)
    {
        var ledger = await ledgerRepository.GetByIdAsync(cmd.LedgerId, ct);
        if (ledger is null)
            return Result.Fail(Error.NotFound<LedgerStream>(cmd.LedgerId.ToString()));

        var archiveResult = ledger.Archive(cmd.WriteKeyPublic);
        if (archiveResult.Success is false)
            return archiveResult;

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
