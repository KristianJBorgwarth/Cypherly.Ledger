using FluentValidation;

namespace Ledger.Application.Commands.Append;

public sealed class AppendLedgerEventCommandValidator : AbstractValidator<AppendLedgerEventCommand>
{
    public AppendLedgerEventCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PreviousHash).NotEmpty();
        RuleFor(x => x.EventHash).NotEmpty();
        RuleFor(x => x.Payload).NotEmpty();
        RuleFor(x => x.WriteKeyPublic).NotEmpty();
        RuleFor(x => x.Signature).NotEmpty();
        RuleForEach(x => x.KeysAdded).NotEmpty();
        RuleForEach(x => x.KeysRemoved).NotEmpty();
    }
}
