using FluentValidation;

namespace Ledger.Application.Commands.Append;

public sealed class AppendLedgerEventCommandValidator : AbstractValidator<AppendLedgerEventCommand>
{
    public AppendLedgerEventCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PreviousHash).Must(h => h.Length == 32);
        RuleFor(x => x.Payload).NotEmpty();
        RuleFor(x => x.WriteKeyPublic).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
        RuleForEach(x => x.KeysAdded).Must(k => k.Length == 32);
        RuleForEach(x => x.KeysRemoved).Must(k => k.Length == 32);
    }
}
