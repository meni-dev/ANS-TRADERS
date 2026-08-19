using Application.DTOs.Customers;
using FluentValidation;

namespace Application.Validators.Customers;

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).PartyName();
        RuleFor(x => x.Phone).PartyPhone();
        RuleFor(x => x.Email).PartyEmail().MaximumLength(200);
        RuleFor(x => x.Gstin).PartyGstin();

        RuleFor(x => x.AddressLine1).MaximumLength(200);
        RuleFor(x => x.AddressLine2).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.StateCode).PartyStateCode();
        RuleFor(x => x.Pincode).PartyPincode();

        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}
