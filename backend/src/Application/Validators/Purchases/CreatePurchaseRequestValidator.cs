using Application.DTOs.Purchases;
using Application.Interfaces;
using FluentValidation;

namespace Application.Validators.Purchases;

public class CreatePurchaseItemRequestValidator : AbstractValidator<CreatePurchaseItemRequest>
{
    public CreatePurchaseItemRequestValidator()
    {
        RuleFor(x => x.ProductId).LineProductId();
        RuleFor(x => x.Quantity).LineQuantity();
        RuleFor(x => x.Rate).LineRate();
        RuleFor(x => x.DiscountPercent).LineDiscountPercent();
    }
}

public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator(IShopClock clock)
    {
        RuleFor(x => x.SupplierId).NotEmpty().WithMessage("Pick a supplier");

        RuleFor(x => x.SupplierInvoiceNumber)
            .NotEmpty().WithMessage("Enter the supplier's bill number")
            .MaximumLength(50);

        RuleFor(x => x.InvoiceDate).DocumentDate(clock);

        RuleFor(x => x.AmountPaid).GreaterThanOrEqualTo(0).WithMessage("Amount paid cannot be negative");

        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Add at least one item");

        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseItemRequestValidator());

        // The same part twice on one bill is almost always a mis-click on the picker rather than a
        // genuine second line, and it makes the stock and credit figures ambiguous.
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("The same product appears on more than one line")
            .When(x => x.Items is { Count: > 0 });
    }
}
