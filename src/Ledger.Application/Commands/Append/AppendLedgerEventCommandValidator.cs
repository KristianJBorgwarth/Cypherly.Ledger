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
        RuleFor(x => x.KeysAdded).NotNull().Must(k => k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.KeysAdded).Must(k => k.Length == 32);
        RuleFor(x => x.InviteKeysAdded).NotNull().Must(k => k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.InviteKeysAdded).Must(k => k.Length == 32);
        RuleFor(x => x.KeysRemoved).NotNull().Must(k => k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.KeysRemoved).Must(k => k.Length == 32);
    }
}
