using Ledger.Application.Abstractions;
using Ledger.Application.Dto;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Queries.GetHead;

public sealed class GetLedgerHeadQueryHandler(
    ILedgerRepository ledgerRepository)
    : IQueryHandler<GetLedgerHeadQuery, LedgerHeadDto>
{
    public async ValueTask<Result<LedgerHeadDto>> Handle(GetLedgerHeadQuery q, CancellationToken ct)
    {
        var ledger = await ledgerRepository.GetByIdAsync(q.LedgerId, ct);

        return ledger is null
            ? Result.Fail<LedgerHeadDto>(Error.NotFound<LedgerStream>(q.LedgerId.ToString()))
            : Result.Ok(LedgerHeadDto.MapFrom(ledger));
    }
}
