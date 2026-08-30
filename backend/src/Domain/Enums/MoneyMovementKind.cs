namespace Domain.Enums;

/// <summary>
/// Money that moves without a customer or a supplier on the other side of it.
/// <para>
/// A receipt settles what somebody owes; an expense buys something. Neither describes taking ₹20,000
/// out of the bank to pay wages, or the owner putting his own money in on day one. Without these the
/// cash book can only ever go down, because card and UPI takings never reach the till while every
/// cash expense leaves it.
/// </para>
/// </summary>
public enum MoneyMovementKind
{
    /// <summary>The notes already in the drawer when the shop started using the app.</summary>
    OpeningFloat,

    /// <summary>Drawn from the bank into the till.</summary>
    BankToCash,

    /// <summary>Banked out of the till.</summary>
    CashToBank,

    /// <summary>The owner putting his own money into the business.</summary>
    CapitalIntroduced,

    /// <summary>The owner taking money out for himself. Not an expense — the business earned it.</summary>
    Drawings,

    /// <summary>
    /// What the stock on the shelf was worth on the day the shop started. Never touches the till;
    /// it exists so "what did I put into this business" has an answer.
    /// </summary>
    OpeningStock,
}
