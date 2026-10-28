using FluentValidation;
using Ledger.Application.Common;

namespace Ledger.Application.Commands.Append;

public sealed class AppendLedgerEventCommandValidator : AbstractValidator<AppendLedgerEventCommand>
{
    public AppendLedgerEventCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PreviousHash).Must(h => h.Length == 32);
        RuleFor(x => x.Payload).NotEmpty().Must(p => p.Length <= LedgerQueryLimits.MaxPayloadBytes);
        RuleFor(x => x.WriteKeyPublic).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
        RuleFor(x => x.KeyChanges).NotNull();
        RuleFor(x => x.KeyChanges.Added).NotNull().Must(k => k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.KeyChanges.Added).Must(k => k.Length == 32);
        RuleFor(x => x.KeyChanges.InvitesAdded).NotNull().Must(k => k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.KeyChanges.InvitesAdded).Must(k => k.Length == 32);
        RuleFor(x => x.KeyChanges.Removed).NotNull().Must(k => k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.KeyChanges.Removed).Must(k => k.Length == 32);
    }
}
