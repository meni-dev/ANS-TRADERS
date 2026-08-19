using Application.DTOs.Stock;
using FluentValidation;

namespace Application.Validators.Stock;

public class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Pick a product");

        RuleFor(x => x.CountedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("A counted quantity cannot be negative");

        // An unexplained correction is indistinguishable from a mistake when it is read back months
        // later, so the reason is required rather than optional. It is now a code as well as a
        // sentence — the service checks it parses, and the loss report counts it.
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Say why the stock is being corrected")
            .MaximumLength(30);

        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
