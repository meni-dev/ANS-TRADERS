using Application.DTOs.Payments;
using Application.Interfaces;
using FluentValidation;

namespace Application.Validators.Payments;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    public CreatePaymentRequestValidator(IShopClock clock)
    {
        RuleFor(x => x.Direction).NotEmpty().WithMessage("Say whether money came in or went out");

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Enter an amount greater than zero");

        RuleFor(x => x.Mode).NotEmpty().WithMessage("Pick how it was paid");

        RuleFor(x => x.PaymentDate).DocumentDate(clock);

        RuleFor(x => x.ReferenceNumber).MaximumLength(60);
        RuleFor(x => x.Notes).MaximumLength(500);

        // A payment with nobody on it cannot be reconciled later, and "Cash" as a stand-in name
        // hides that the shop never captured who paid.
        RuleFor(x => x.WalkInName)
            .NotEmpty().WithMessage("Enter a name, or pick a saved customer")
            .MaximumLength(200)
            .When(x => x.CustomerId is null && x.SupplierId is null);

        RuleForEach(x => x.Allocations).ChildRules(allocation =>
        {
            allocation.RuleFor(a => a.DocumentId).NotEmpty().WithMessage("Pick a document");
            allocation.RuleFor(a => a.Amount).GreaterThan(0).WithMessage("Allocate more than zero");
        });

        When(x => x.Cheque is not null, () =>
        {
            RuleFor(x => x.Cheque!.ChequeNumber)
                .NotEmpty().WithMessage("Enter the cheque number")
                .MaximumLength(20);

            RuleFor(x => x.Cheque!.BankName)
                .NotEmpty().WithMessage("Enter the bank the cheque is drawn on")
                .MaximumLength(100);
        });
    }
}
