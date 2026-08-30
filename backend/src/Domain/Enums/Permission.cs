namespace Domain.Enums;

/// <summary>
/// The things a person can be allowed to do.
/// <para>
/// <b>This list lives in code, not in the database, and that is deliberate.</b> A permission is only
/// real because somewhere a service refuses to run without it. If the shop could invent new ones, it
/// would end up with rows that look like protection and stop nothing. Roles are the part the shop
/// builds; permissions are the part the code guarantees.
/// </para>
/// <para>
/// Stored by name rather than by number, so inserting a member later cannot silently hand somebody
/// a permission they were never given.
/// </para>
/// </summary>
public enum Permission
{
    // ------------------------------------------------------------------ Sales
    BillCreate,
    BillCancel,

    /// <summary>Issuing and cancelling credit notes — goods coming back over the counter.</summary>
    SalesReturn,

    /// <summary>Taking money off a whole bill. The commonest way a counter leaks money.</summary>
    BillDiscount,

    // --------------------------------------------------------------- Purchase
    /// <summary>Reading purchase bills — which means reading what the shop pays for its parts.</summary>
    PurchaseView,
    PurchaseCreate,
    PurchaseCancel,
    PurchaseReturn,

    // ------------------------------------------------------------------ Stock
    StockView,

    /// <summary>Correcting the shelf. Every use of this writes an audit row naming who did it.</summary>
    StockAdjust,

    /// <summary>The catalogue, including rates — so it carries the same weight as seeing cost.</summary>
    ProductManage,

    // ------------------------------------------------------------------ Money
    PaymentRecord,
    PaymentCancel,
    ExpenseRecord,
    CashDayClose,

    /// <summary>
    /// Moving money between the bank and the till, and recording what the owner put in or took out.
    /// Its own permission because it is the owner's money, not the shop's trade.
    /// </summary>
    CapitalMovement,

    // ------------------------------------------------- The sensitive numbers
    /// <summary>
    /// Buying price, margin, profit. The counter needs none of it to sell a part, and this is the
    /// permission most shops actually want when they ask for roles.
    /// </summary>
    CostView,

    /// <summary>Registers and the GST return shapes — the whole shop's trade in one table.</summary>
    ReportView,

    // ------------------------------------------------------------------ Admin
    UserManage,
    SettingsEdit,
    BooksLock,
    AuditView,
}
