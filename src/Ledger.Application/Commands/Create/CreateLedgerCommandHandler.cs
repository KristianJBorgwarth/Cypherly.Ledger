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
    public async ValueTask<Result> Handle(CreateLedgerCommand cmd, CancellationToken ct)
    {
        var existingLedger = await ledgerRepository.GetByIdAsync(cmd.LedgerId, ct);

        if(signatureHelper.IsValidSignature(cmd.LedgerId, 0, [], cmd.Payload, cmd.WriteKey, cmd.Signature) is false)
            return Result.Fail(Error.Validation("Invalid signature."));

        if(existingLedger is not null)
            return Result.Fail(Error.Conflict("Ledger already exists."));

        var evtHash = signatureHelper.GenerateHash(cmd.LedgerId, 0, [], cmd.Payload);

        var ledger = LedgerStream.Initialize(
            cmd.LedgerId,
            cmd.Payload,
            cmd.WriteKey,
            evtHash,
            cmd.Signature);

        await ledgerRepository.CreateAsync(ledger, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
