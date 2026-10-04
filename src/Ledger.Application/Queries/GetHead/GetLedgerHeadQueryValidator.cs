using FluentValidation;

namespace Ledger.Application.Queries.GetHead;

public sealed class GetLedgerHeadQueryValidator : AbstractValidator<GetLedgerHeadQuery>
{
    public GetLedgerHeadQueryValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
    }
}
