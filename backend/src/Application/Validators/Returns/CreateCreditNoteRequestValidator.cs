using Application.DTOs.Returns;
using Application.Interfaces;
using FluentValidation;

namespace Application.Validators.Returns;

/// <summary>
/// Shape only. Whether a quantity can actually come back depends on what has already been returned,
/// which needs the invoice — that check lives in the service, next to the data it reads.
/// </summary>
public class CreateCreditNoteRequestValidator : AbstractValidator<CreateCreditNoteRequest>
{
    public CreateCreditNoteRequestValidator(IShopClock clock)
    {
        RuleFor(r => r.InvoiceId).NotEmpty().WithMessage("Which bill are these goods coming back on?");

        RuleFor(r => r.NoteDate)
            .Must(d => d <= clock.Today)
            .WithMessage("A credit note cannot be dated in the future");

        // Required on the printed note under Rule 53(1A)(g), and the first thing an auditor asks.
        RuleFor(r => r.Reason)
            .NotEmpty().WithMessage("Say why the goods came back")
            .MaximumLength(500);

        RuleFor(r => r.Lines).NotEmpty().WithMessage("Nothing is coming back on this note");

        RuleForEach(r => r.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.DocumentItemId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThanOrEqualTo(0);
        });

        RuleFor(r => r.RefundAmount)
            .GreaterThanOrEqualTo(0).When(r => r.RefundAmount.HasValue)
            .WithMessage("A refund cannot be negative");
    }
}
