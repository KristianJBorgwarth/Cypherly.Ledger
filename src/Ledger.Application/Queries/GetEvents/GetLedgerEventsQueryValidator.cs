using FluentValidation;
using Ledger.Application.Common;

namespace Ledger.Application.Queries.GetEvents;

public sealed class GetLedgerEventsQueryValidator : AbstractValidator<GetLedgerEventsQuery>
{
    public GetLedgerEventsQueryValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.FromVersion).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, LedgerQueryLimits.MaxEventsPerRead);
    }
}
