using Ledger.Application.Abstractions;
using Ledger.Application.Interfaces;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Commands.Create;

public sealed class CreateLedgerCommandHandler(
    ILedgerRepository ledgerRepository,
    ISignatureHelper signatureHelper,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateLedgerCommand>
{
    private const int FirstVersion = 1;
    private static readonly byte[] FirstPreviousHash = new byte[32];

    public async ValueTask<Result> Handle(CreateLedgerCommand cmd, CancellationToken ct)
    {
        var existingLedger = await ledgerRepository.GetByIdAsync(cmd.LedgerId, ct);

        if (signatureHelper.IsValidSignature(
                ledgerId: cmd.LedgerId,
                version: FirstVersion,
                previousHash: FirstPreviousHash,
                payload: cmd.Payload,
                writeKeyPublic: cmd.WriteKey,
                signature: cmd.Signature,
                keysAdded: cmd.KeysAdded,
                keysRemoved: []) is false)
            return Result.Fail(Error.Forbidden("Invalid signature."));

        if(existingLedger is not null)
            return Result.Fail(Error.Conflict("Ledger already exists."));

        var evtHash = signatureHelper.GenerateHash(
            ledgerId: cmd.LedgerId,
            version: FirstVersion,
            previousHash: FirstPreviousHash,
            payload: cmd.Payload,
            keysAdded: cmd.KeysAdded,
            keysRemoved: []);

        var ledgerResult = LedgerStream.Initialize(
            id: cmd.LedgerId,
            creatorWriteKey: cmd.WriteKey,
            payload: cmd.Payload,
            eventhash: evtHash,
            signature: cmd.Signature,
            keysAdded: cmd.KeysAdded);

        if(ledgerResult.Success is false)
            return ledgerResult;

        await ledgerRepository.CreateAsync(ledgerResult.RequiredValue, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
