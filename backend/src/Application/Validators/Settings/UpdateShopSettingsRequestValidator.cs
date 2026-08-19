using Application.DTOs.Settings;
using FluentValidation;

namespace Application.Validators.Settings;

public class UpdateShopSettingsRequestValidator : AbstractValidator<UpdateShopSettingsRequest>
{
    public UpdateShopSettingsRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The shop name is printed on every bill")
            .MaximumLength(200);

        RuleFor(x => x.LegalName).MaximumLength(200);

        // Reuses the party rules, so the shop's own GSTIN is held to exactly the standard its
        // customers and suppliers are.
        RuleFor(x => x.Gstin).PartyGstin();
        RuleFor(x => x.Email).PartyEmail();
        RuleFor(x => x.Pincode).PartyPincode();

        RuleFor(x => x.StateCode)
            .NotEmpty().WithMessage("The state code decides IGST against CGST + SGST")
            .Matches("^[0-9]{2}$").WithMessage("State code must be 2 digits");

        RuleFor(x => x.State).NotEmpty().WithMessage("State is required").MaximumLength(100);

        RuleFor(x => x.AddressLine1).MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(20);

        RuleFor(x => x.InvoiceFooter).MaximumLength(500);
        RuleFor(x => x.BankDetails).MaximumLength(500);
        RuleFor(x => x.InvoiceTerms).MaximumLength(1000);

        RuleFor(x => x.InvoiceTemplate).NotEmpty().WithMessage("Pick an invoice template");
    }
}
