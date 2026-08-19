namespace Application.Common;

/// <summary>Per-line money figures, all already rounded to paise.</summary>
public readonly record struct LineAmounts(
    decimal GrossAmount,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal LineTotal,
    /// <summary>
    /// This line's share of a discount given on the bill as a whole. Kept apart from
    /// <see cref="DiscountAmount"/>, which is the discount the counter gave on this line — the two
    /// are different conversations and a customer disputing one should not be shown the other.
    /// </summary>
    decimal BillDiscountShare = 0m);

/// <summary>Document-level roll-up of every <see cref="LineAmounts"/> on a bill.</summary>
public readonly record struct DocumentAmounts(
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxableAmount,
    decimal CgstAmount,
    decimal SgstAmount,
    decimal IgstAmount,
    decimal TotalTax,
    decimal RoundOff,
    decimal GrandTotal);

/// <summary>
/// The single place GST is worked out. Purchases and sales run through the same code so a bill and
/// the invoice raised against the same goods can never disagree by a paisa.
/// </summary>
public static class GstCalculator
{
    /// <param name="billDiscountShare">
    /// A flat amount off this line's taxable value, on top of its own discount — this line's slice
    /// of a discount given on the whole bill. It is deducted <b>before</b> tax because GST is
    /// charged on the transaction value, and a discount shown on the invoice reduces that value.
    /// Taking it off the total afterwards would leave the shop paying tax on money it never took.
    /// </param>
    public static LineAmounts ComputeLine(
        decimal quantity,
        decimal rate,
        decimal discountPercent,
        decimal gstRate,
        bool isInterState,
        decimal billDiscountShare = 0m)
    {
        var gross = Round(quantity * rate);
        var discount = Round(gross * discountPercent / 100m);
        var share = Round(billDiscountShare);
        var taxable = gross - discount - share;
        var tax = Round(taxable * gstRate / 100m);

        decimal cgst = 0m, sgst = 0m, igst = 0m;

        if (isInterState)
        {
            igst = tax;
        }
        else
        {
            // The remainder goes to SGST rather than rounding both halves independently: at an odd
            // paisa (say ₹4.05 of tax) two rounded halves come to ₹4.06 and the line stops adding up.
            cgst = Round(tax / 2m);
            sgst = tax - cgst;
        }

        return new LineAmounts(gross, discount, taxable, cgst, sgst, igst, taxable + tax, share);
    }

    public static DocumentAmounts ComputeDocument(IReadOnlyCollection<LineAmounts> lines)
    {
        var subTotal = lines.Sum(l => l.GrossAmount);

        // Both discounts roll into the one figure the bill prints as "Discount", because that is
        // what the customer sees taken off. Which part came from where is on the lines.
        var discount = lines.Sum(l => l.DiscountAmount) + lines.Sum(l => l.BillDiscountShare);
        var taxable = lines.Sum(l => l.TaxableAmount);
        var cgst = lines.Sum(l => l.CgstAmount);
        var sgst = lines.Sum(l => l.SgstAmount);
        var igst = lines.Sum(l => l.IgstAmount);
        var totalTax = cgst + sgst + igst;

        var beforeRounding = taxable + totalTax;

        // Counter bills are collected in whole rupees, and the difference is shown on its own line
        // so the customer can see why the total is not the sum of the parts.
        var grandTotal = Math.Round(beforeRounding, 0, MidpointRounding.AwayFromZero);
        var roundOff = grandTotal - beforeRounding;

        return new DocumentAmounts(
            subTotal, discount, taxable, cgst, sgst, igst, totalTax, roundOff, grandTotal);
    }

    /// <summary>
    /// A supply is inter-state when the two parties sit in different GST states. An unknown state
    /// code on the other party is treated as local, which is the safe default for a counter sale to
    /// a walk-in with no GSTIN.
    /// </summary>
    /// <summary>
    /// Splits a bill-level discount across lines in proportion to what each contributes, and
    /// recomputes their tax.
    /// <para>
    /// Proportional to taxable value rather than spread evenly: a ₹50 discount on a bill of one
    /// ₹900 part and one ₹100 part belongs almost entirely to the first, and splitting it evenly
    /// would move ₹22 of taxable value between two different GST rates.
    /// </para>
    /// <para>
    /// The rounding remainder goes to the largest line — the same reasoning as the CGST/SGST split.
    /// Rounding each share independently leaves the shares not adding up to the discount, and then
    /// the bill's own total disagrees with the discount printed on it.
    /// </para>
    /// </summary>
    public static IReadOnlyList<LineAmounts> ApplyBillDiscount(
        IReadOnlyList<(decimal Quantity, decimal Rate, decimal DiscountPercent, decimal GstRate)> lines,
        decimal billDiscount,
        bool isInterState)
    {
        var computed = lines
            .Select(l => ComputeLine(l.Quantity, l.Rate, l.DiscountPercent, l.GstRate, isInterState))
            .ToList();

        var discount = Round(billDiscount);
        var totalTaxable = computed.Sum(l => l.TaxableAmount);

        if (discount <= 0 || totalTaxable <= 0)
        {
            return computed;
        }

        var shares = computed
            .Select(l => Round(discount * l.TaxableAmount / totalTaxable))
            .ToList();

        var largest = computed
            .Select((line, index) => (line.TaxableAmount, index))
            .OrderByDescending(x => x.TaxableAmount)
            .First().index;

        shares[largest] += discount - shares.Sum();

        return lines
            .Select((l, i) => ComputeLine(
                l.Quantity, l.Rate, l.DiscountPercent, l.GstRate, isInterState, shares[i]))
            .ToList();
    }

    public static bool IsInterState(string? sellerStateCode, string? partyStateCode)
    {
        if (string.IsNullOrWhiteSpace(sellerStateCode) || string.IsNullOrWhiteSpace(partyStateCode))
        {
            return false;
        }

        return !string.Equals(sellerStateCode.Trim(), partyStateCode.Trim(), StringComparison.Ordinal);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
