using Application.DTOs.Products;
using FluentValidation;

namespace Application.Validators.Products;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.PartNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Uqc).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Hsn).NotEmpty().MaximumLength(20);

        RuleFor(x => x.GstRate).InclusiveBetween(0, 100);
        RuleFor(x => x.PurchaseRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}
