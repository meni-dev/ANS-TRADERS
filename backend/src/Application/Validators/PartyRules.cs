using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Field rules shared by the customer and supplier validators. Customers and suppliers are
/// separate aggregates, but the contact and GST identity fields are the same shape on both, and
/// a GSTIN that validates on one form must validate identically on the other.
/// </summary>
public static class PartyRules
{
    // Optional fields accept an empty string as "not supplied", so each pattern carries its own
    // `^$` alternative. FluentValidation's When() takes the whole model rather than the property
    // being validated, which makes it awkward to express as a reusable rule-builder extension.
    private const string Optional = "^$|";

    /// <summary>
    /// 15 characters: 2-digit state code, 10-character PAN, 1-digit entity number, a literal 'Z',
    /// then a checksum character.
    /// </summary>
    private const string GstinPattern = Optional + "^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$";

    private const string EmailPattern = Optional + @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    private const string PincodePattern = Optional + "^[0-9]{6}$";
    private const string StateCodePattern = Optional + "^[0-9]{2}$";
    private const string PhonePattern = "^[0-9]{10,15}$";

    public static IRuleBuilderOptions<T, string> PartyName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Name is required").MaximumLength(200);

    public static IRuleBuilderOptions<T, string> PartyPhone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Phone is required")
            .Matches(PhonePattern).WithMessage("Enter a valid phone number (10-15 digits)");

    public static IRuleBuilderOptions<T, string?> PartyGstin<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(GstinPattern).WithMessage("Enter a valid 15-character GSTIN");

    public static IRuleBuilderOptions<T, string?> PartyEmail<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(EmailPattern).WithMessage("Enter a valid email address");

    public static IRuleBuilderOptions<T, string?> PartyPincode<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(PincodePattern).WithMessage("Pincode must be 6 digits");

    public static IRuleBuilderOptions<T, string?> PartyStateCode<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(StateCodePattern).WithMessage("State code must be 2 digits");
}
