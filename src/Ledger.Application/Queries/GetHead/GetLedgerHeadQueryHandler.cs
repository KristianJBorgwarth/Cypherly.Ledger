using Ledger.Application.Abstractions;
using Ledger.Application.Dto;
using Ledger.Application.Interfaces;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Queries.GetHead;

public sealed class GetLedgerHeadQueryHandler(
    ILedgerRepository ledgerRepository,
    ISignatureHelper signatureHelper)
    : IQueryHandler<GetLedgerHeadQuery, LedgerHeadDto>
{
    public async ValueTask<Result<LedgerHeadDto>> Handle(GetLedgerHeadQuery q, CancellationToken ct)
    {
        var ledger = await ledgerRepository.GetAsync(new LedgerWithWriteKeysSpec(q.LedgerId), ct);

        return ledger is null
               || !ledger.HasWriteKey(q.WriteKeyPublic)
               || !signatureHelper.IsValidReadSignature(q.LedgerId, q.Timestamp, q.WriteKeyPublic, q.Signature)
            ? Result.Fail<LedgerHeadDto>(Error.NotFound<LedgerStream>(q.LedgerId.ToString()))
            : Result.Ok(LedgerHeadDto.MapFrom(ledger));
    }
}
