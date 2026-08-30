using FluentValidation;

namespace Application.Validators;

/// <summary>
/// The GST rules that are the same wherever they are applied — a rate, an HSN, a GSTIN check digit.
/// </summary>
public static class GstRules
{
    /// <summary>
    /// The slabs India actually has. Anything else is a typo that reaches GSTR-1 before anybody
    /// notices — and by then every bill on that part has been issued at the wrong rate.
    /// </summary>
    private static readonly decimal[] Slabs = [0m, 0.25m, 3m, 5m, 12m, 18m, 28m];

    public static IRuleBuilderOptions<T, decimal> GstSlab<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(rate => Slabs.Contains(rate))
            .WithMessage("GST is 0, 0.25, 3, 5, 12, 18 or 28 percent — nothing in between");

    /// <summary>
    /// An HSN is four, six or eight digits. The portal rejects a return carrying anything else, and
    /// it rejects the whole file rather than the one line.
    /// </summary>
    public static IRuleBuilderOptions<T, string> Hsn<T>(this IRuleBuilder<T, string> rule) =>
        rule.Matches("^[0-9]{4}([0-9]{2}([0-9]{2})?)?$")
            .WithMessage("An HSN is 4, 6 or 8 digits");

    /// <summary>
    /// The 15th character of a GSTIN is a check digit over the other fourteen. Checking it turns a
    /// mistyped GSTIN into a message at the counter, instead of a bill the customer cannot claim
    /// input credit on and a mismatch somebody chases months later.
    /// </summary>
    /// <summary>
    /// The first two characters of a GSTIN are the state code. A record whose state field says one
    /// thing and whose GSTIN says another produces a bill charging tax for one state while carrying
    /// a registration from a different one — and GSTR-1 is matched on that number, so the other
    /// side's return will never reconcile.
    /// </summary>
    public static bool StateMatches(string? gstin, string? stateCode) =>
        string.IsNullOrWhiteSpace(gstin)
        || string.IsNullOrWhiteSpace(stateCode)
        || gstin.Length < 2
        || gstin[..2] == stateCode.Trim().PadLeft(2, '0');

    public static bool HasValidChecksum(string gstin)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        if (gstin.Length != 15)
        {
            return false;
        }

        var total = 0;

        for (var i = 0; i < 14; i++)
        {
            var value = alphabet.IndexOf(gstin[i]);

            if (value < 0)
            {
                return false;
            }

            // Every second character counts double, and a product that overflows the alphabet wraps
            // — the same shape as the Luhn check on a card number.
            var product = value * (i % 2 == 0 ? 1 : 2);
            total += (product / alphabet.Length) + (product % alphabet.Length);
        }

        var expected = alphabet[(alphabet.Length - (total % alphabet.Length)) % alphabet.Length];

        return gstin[14] == expected;
    }
}
