namespace Domain.Enums;

/// <summary>
/// What the money went on. Coded rather than free text so "how much did I spend on rent this year"
/// is a query — the same reason stock adjustments carry a reason rather than a sentence.
/// <para>
/// Deliberately short. A shop that has to choose between fourteen categories stops choosing and
/// puts everything in Other, and then the list has told you nothing.
/// </para>
/// </summary>
public enum ExpenseCategory
{
    /// <summary>Shop rent and anything the landlord charges.</summary>
    Rent = 0,

    /// <summary>Wages, bonus, ESI/PF — anything paid to staff.</summary>
    Salary = 1,

    /// <summary>Electricity, water, phone, internet.</summary>
    Utilities = 2,

    /// <summary>Carriage on goods bought, courier, local transport.</summary>
    Freight = 3,

    /// <summary>Packing, stationery, cleaning, tea — the small daily spend.</summary>
    ShopExpenses = 4,

    /// <summary>Bank charges, commission, interest paid.</summary>
    BankCharges = 5,

    /// <summary>Advertising, boards, offers.</summary>
    Marketing = 6,

    /// <summary>GST paid, professional tax, licence fees.</summary>
    TaxesAndFees = 7,

    /// <summary>Shop or vehicle repairs, tools, maintenance.</summary>
    Repairs = 8,

    Other = 9,
}
