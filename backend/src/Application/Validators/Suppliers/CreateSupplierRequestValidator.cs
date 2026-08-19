using Application.DTOs.Suppliers;
using FluentValidation;

namespace Application.Validators.Suppliers;

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name).PartyName();
        RuleFor(x => x.Phone).PartyPhone();
        RuleFor(x => x.Email).PartyEmail().MaximumLength(200);
        RuleFor(x => x.Gstin).PartyGstin();
        RuleFor(x => x.ContactPerson).MaximumLength(200);

        RuleFor(x => x.AddressLine1).MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.StateCode).PartyStateCode();
        RuleFor(x => x.Pincode).PartyPincode();
        RuleFor(x => x.PaymentTerms).MaximumLength(100);

        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}
