namespace Domain.Enums;

/// <summary>
/// What kind of supply a part is, for the return.
/// <para>
/// A rate of zero is not enough to tell these apart, and GSTR-1 keeps them in different tables. Nil
/// rated and exempt both charge nothing but are reported separately; non-GST goods are outside the
/// Act altogether. Reporting any of them inside taxable turnover overstates what the shop sold.
/// </para>
/// </summary>
public enum SupplyType
{
    /// <summary>Ordinary goods at a positive rate.</summary>
    Taxable,

    /// <summary>Rated at nil in the tariff. GSTR-1 Table 8, GSTR-3B 3.1(c).</summary>
    NilRated,

    /// <summary>Exempted by notification. Same tables, its own column.</summary>
    Exempt,

    /// <summary>Outside GST entirely — petrol, diesel, alcohol.</summary>
    NonGst,
}
