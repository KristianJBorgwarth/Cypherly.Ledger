using FluentValidation;
using Ledger.Application.Commands.Archive;

public sealed class ArchiveLedgerCommandValidator : AbstractValidator<ArchiveLedgerCommand>
{
    public ArchiveLedgerCommandValidator()
    {
        RuleFor(x => x.LedgerId).NotEmpty();
        RuleFor(x => x.WriteKeyPublic).Must(k => k.Length == 32);
        RuleFor(x => x.Signature).Must(s => s.Length == 64);
    }
}
