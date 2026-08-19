using Application.DTOs.Invoices;
using FluentValidation;

namespace Application.Validators.Invoices;

public class CreateInvoiceItemRequestValidator : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(x => x.ProductId).LineProductId();
        RuleFor(x => x.Quantity).LineQuantity();
        RuleFor(x => x.Rate).LineRate();
        RuleFor(x => x.DiscountPercent).LineDiscountPercent();
    }
}

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        // Either an account customer or a name for the walk-in. A bill with no one on it cannot be
        // reconciled later, and "Cash" as a default name hides that the shop never captured who bought.
        RuleFor(x => x.WalkInName)
            .NotEmpty().WithMessage("Enter a customer name, or pick a saved customer")
            .MaximumLength(200)
            .When(x => x.CustomerId is null);

        RuleFor(x => x.InvoiceDate).DocumentDate();

        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0).WithMessage("Amount paid cannot be negative");

        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Add at least one item");

        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemRequestValidator());

        // See the note on the purchase validator.
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("The same product appears on more than one line")
            .When(x => x.Items is { Count: > 0 });
    }
}
