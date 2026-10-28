using FluentValidation;
using Ledger.Application.Common;

namespace Ledger.Application.Commands.Create;

public sealed class CreateLedgerCommandValidator : AbstractValidator<CreateLedgerCommand>
{
    private const int MinKeysAdded = 3;

    public CreateLedgerCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.Payload).NotEmpty().Must(p => p.Length <= LedgerQueryLimits.MaxPayloadBytes);
        RuleFor(x => x.WriteKey).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
        RuleFor(x => x.InviteKeysAdded).NotNull().Must(k => k.Count >= MinKeysAdded && k.Count <= LedgerQueryLimits.MaxKeysPerEvent);
        RuleForEach(x => x.InviteKeysAdded).Must(k => k.Length == 32);
    }
}

