using Ledger.Application.Abstractions;
using Ledger.Application.Exceptions;
using Ledger.Application.Interfaces;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Archive;

public sealed class ArchiveLedgerCommandHandler(
    ILedgerRepository ledgerRepository,
    ISignatureHelper signatureHelper,
    IUnitOfWork unitOfWork) 
    : ICommandHandler<ArchiveLedgerCommand>
{
    public async ValueTask<Result> Handle(ArchiveLedgerCommand cmd, CancellationToken ct)
    {
        var ledger = await ledgerRepository.GetByIdAsync(cmd.LedgerId, ct);
        if (ledger is null)
            return Result.Fail(Error.NotFound<LedgerStream>(cmd.LedgerId.ToString()));

        var eventVersion = cmd.ExpectedVersion + 1;

        if (signatureHelper.IsValidSignature(
                ledgerId: cmd.LedgerId,
                version: eventVersion,
                previousHash: cmd.PreviousHash,
                payload: cmd.Payload,
                writeKeyPublic: cmd.WriteKeyPublic,
                signature: cmd.Signature,
                keysAdded: [],
                keysRemoved: []) is false)
            return Result.Fail(Error.Forbidden("Invalid signature."));

        var evtHash = signatureHelper.GenerateHash(
            ledgerId: cmd.LedgerId,
            version: eventVersion,
            previousHash: cmd.PreviousHash,
            payload: cmd.Payload,
            keysAdded: [],
            keysRemoved: []);

        var archiveResult = ledger.Archive(
            expectedVersion: cmd.ExpectedVersion,
            previousHash: cmd.PreviousHash,
            eventHash: evtHash,
            payload: cmd.Payload,
            writeKeyPublic: cmd.WriteKeyPublic,
            signature: cmd.Signature);

        if (archiveResult.Success is false)
            return archiveResult;

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException e)
        {
            return Result.Fail(Error.Conflict(e.Message));
        }

        return Result.Ok();
    }
}
