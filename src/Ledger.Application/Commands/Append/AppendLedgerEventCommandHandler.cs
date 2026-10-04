using Ledger.Application.Abstractions;
using Ledger.Application.Exceptions;
using Ledger.Application.Interfaces;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Append;

public sealed class AppendLedgerEventCommandHandler(
    ILedgerRepository ledgerRepository,
    ISignatureHelper signatureHelper,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AppendLedgerEventCommand>
{
    public async ValueTask<Result> Handle(AppendLedgerEventCommand cmd, CancellationToken ct)
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
                keysAdded: cmd.KeysAdded,
                keysRemoved: cmd.KeysRemoved) is false)
            return Result.Fail(Error.Forbidden("Invalid signature."));

        var evtHash = signatureHelper.GenerateHash(
            ledgerId: cmd.LedgerId,
            version: eventVersion,
            previousHash: cmd.PreviousHash,
            payload: cmd.Payload,
            keysAdded: cmd.KeysAdded,
            keysRemoved: cmd.KeysRemoved);

        var result = ledger.Append(
            expectedVersion: cmd.ExpectedVersion,
            previousHash: cmd.PreviousHash,
            eventHash: evtHash,
            payload: cmd.Payload,
            writeKeyPublic: cmd.WriteKeyPublic,
            signature: cmd.Signature,
            keysAdded: cmd.KeysAdded,
            keysRemoved: cmd.KeysRemoved);

        if(result.Success is false)
            return result;

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
