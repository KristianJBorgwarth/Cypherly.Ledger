using FluentValidation;

namespace Ledger.Application.Commands.Create;

public sealed class CreateLedgerCommandValidator : AbstractValidator<CreateLedgerCommand>
{
    private const int MaxPayloadBytes = 64 * 1024;

    public CreateLedgerCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.Payload).NotEmpty().Must(p => p.Length <= MaxPayloadBytes);
        RuleFor(x => x.WriteKey).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
    }
}

