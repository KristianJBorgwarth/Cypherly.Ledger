using Ledger.Application.Abstractions;
using Ledger.Application.Dto;
using Ledger.Application.Interfaces;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;

namespace Ledger.Application.Queries.GetEvents;

public sealed class GetLedgerEventsQueryHandler(
    ILedgerRepository ledgerRepository,
    ISignatureHelper signatureHelper)
    : IQueryHandler<GetLedgerEventsQuery, LedgerEventsDto>
{
    public async ValueTask<Result<LedgerEventsDto>> Handle(GetLedgerEventsQuery q, CancellationToken ct)
    {
        var ledger = await ledgerRepository.GetAsync(new LedgerWithWriteKeysSpec(q.LedgerId), ct);

        if (ledger is null
            || !ledger.HasWriteKey(q.WriteKeyPublic)
            || !signatureHelper.IsValidReadSignature(q.LedgerId, q.Timestamp, q.WriteKeyPublic, q.Signature))
            return Result.Fail<LedgerEventsDto>(Error.NotFound<LedgerStream>(q.LedgerId.ToString()));

        var events = await ledgerRepository.GetEventsAsync(q.LedgerId, q.FromVersion, q.Limit + 1, ct);

        var hasMore = events.Count > q.Limit;

        return Result.Ok(new LedgerEventsDto
        {
            Version = ledger.Version,
            Archived = ledger.Archived,
            HasMore = hasMore,
            Events = [.. events.Take(q.Limit).Select(LedgerEventDto.MapFrom)],
        });
    }
}
