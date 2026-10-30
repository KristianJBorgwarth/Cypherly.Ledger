using FluentValidation;
using Ledger.Application.Common;

namespace Ledger.Application.Commands.Create;

public sealed class CreateLedgerCommandValidator : AbstractValidator<CreateLedgerCommand>
{
    private const int MinKeysAdded = 2;

    public CreateLedgerCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.Payload).NotEmpty().Must(p => p.Length <= LedgerQueryLimits.MaxPayloadBytes);
        RuleFor(x => x.WriteKey).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
        RuleFor(x => x.KeyChanges).NotNull();
        RuleFor(x => x.KeyChanges.InvitesAdded).NotNull().Must(k => k.Count >= MinKeysAdded && k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.KeyChanges.InvitesAdded).Must(k => k.Length == 32);
        RuleFor(x => x.KeyChanges.Added).Empty();
        RuleFor(x => x.KeyChanges.Removed).Empty();
    }
}

