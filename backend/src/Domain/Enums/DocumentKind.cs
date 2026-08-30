namespace Domain.Enums;

/// <summary>
/// The document series a shop keeps. Each one numbers itself independently, and restarts at 1 every
/// financial year.
/// </summary>
public enum DocumentKind
{
    Invoice,
    Purchase,
    CreditNote,
    DebitNote,
    Expense,

    /// <summary>Money coming in.</summary>
    Receipt,

    /// <summary>Money going out.</summary>
    PaymentOut,
}
