namespace Domain.Enums;

/// <summary>
/// The actions worth logging. Creating a bill is not one of them — the bill is its own record.
/// These are the things that undo, hide, or change the rules.
/// </summary>
public enum AuditAction
{
    Cancelled = 0,
    StockAdjusted = 1,
    DiscountGiven = 2,
    BooksLocked = 3,
    BooksUnlocked = 4,
    UserCreated = 5,
    UserDeactivated = 6,
    PasswordChanged = 7,
    SignedIn = 8,
    CatalogueImported = 9,

    /// <summary>A role was created, retrimmed or removed — who may do what, changed.</summary>
    RoleChanged = 10,

    /// <summary>Somebody was moved from one role to another.</summary>
    UserRoleChanged = 11,

    /// <summary>Money moved between the bank and the till, or in and out of the business.</summary>
    MoneyMoved = 12,

    /// <summary>The shop's own details changed — its GSTIN, its state, its name.</summary>
    SettingsChanged = 13,
}
