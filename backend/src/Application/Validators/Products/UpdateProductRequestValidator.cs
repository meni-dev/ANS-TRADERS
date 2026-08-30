using Application.DTOs.Products;
using Domain.Enums;
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
        RuleFor(x => x.Hsn).NotEmpty().MaximumLength(20).Hsn();

        RuleFor(x => x.GstRate).GstSlab();
        RuleFor(x => x.PurchaseRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Mrp).GreaterThanOrEqualTo(0);

        // Billing above MRP is refused under the Legal Metrology rules, so a selling price above it
        // is a price that can never be used. Catching it here means finding out when the part is
        // set up, rather than with a customer waiting at the counter.
        RuleFor(x => x.SellingRate)
            .LessThanOrEqualTo(x => x.Mrp)
            .When(x => x.Mrp > 0)
            .WithMessage("Selling rate cannot be above the MRP — a bill at that price would be refused");
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);

        // A rate of zero and a taxable classification cannot both be true, and neither can a
        // positive rate on goods that are outside GST. Catching it here keeps the return's tables
        // from disagreeing with the tariff.
        RuleFor(x => x.SupplyType)
            .Must(v => v is null || Enum.TryParse<SupplyType>(v, ignoreCase: true, out _))
            .WithMessage("Supply type is Taxable, NilRated, Exempt or NonGst");

        RuleFor(x => x.GstRate)
            .Equal(0m)
            .When(x => !IsTaxable(x.SupplyType))
            .WithMessage("Nil rated, exempt and non-GST goods carry no rate");

        RuleFor(x => x.SupplyType)
            .Must(v => IsTaxable(v))
            .When(x => x.GstRate > 0)
            .WithMessage("A part with a rate on it is a taxable supply");

        // The other direction. Zero is a slab in the sense that the tariff lists it, but goods
        // taxed at nothing are nil rated — calling them taxable puts them in the wrong table and
        // inflates the turnover the shop declares.
        RuleFor(x => x.GstRate)
            .GreaterThan(0m)
            .When(x => IsTaxable(x.SupplyType))
            .WithMessage("A taxable part carries a rate. Goods at nothing are nil rated or exempt");
    }

    private static bool IsTaxable(string? supplyType) =>
        string.IsNullOrWhiteSpace(supplyType)
        || string.Equals(supplyType, nameof(SupplyType.Taxable), StringComparison.OrdinalIgnoreCase);
}
