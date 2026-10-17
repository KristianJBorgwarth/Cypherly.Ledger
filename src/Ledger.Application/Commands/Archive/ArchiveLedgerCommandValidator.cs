using FluentValidation;
using Ledger.Application.Common;

namespace Ledger.Application.Commands.Archive;

public sealed class ArchiveLedgerCommandValidator : AbstractValidator<ArchiveLedgerCommand>
{
    public ArchiveLedgerCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PreviousHash).Must(h => h.Length == 32);
        RuleFor(x => x.Payload).NotEmpty().Must(p => p.Length <= LedgerQueryLimits.MaxPayloadBytes);
        RuleFor(x => x.WriteKeyPublic).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
    }
}
