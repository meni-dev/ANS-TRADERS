using Application.DTOs.Roles;
using Domain.Enums;

namespace Application.Common;

/// <summary>
/// What each permission is called and what it actually lets somebody do.
/// <para>
/// Written out here rather than derived from the enum name, because the roles screen is where a shop
/// owner decides who can see the buying price — and "CostView" is not a sentence anybody can make
/// that decision from.
/// </para>
/// </summary>
public static class PermissionCatalogue
{
    private static readonly (Permission Permission, string Group, string Label, string Description)[] Entries =
    [
        (Permission.BillCreate, "Sales", "Raise a bill", "Sell over the counter and print the invoice"),
        (Permission.BillCancel, "Sales", "Cancel a bill", "Void an issued bill. Always logged with their name"),
        (Permission.SalesReturn, "Sales", "Take goods back", "Issue and cancel credit notes against a bill"),
        (Permission.BillDiscount, "Sales", "Give a bill discount", "Take money off the whole bill, not just a line"),

        (Permission.PurchaseView, "Purchase", "See purchase bills", "Which means seeing what the shop pays its suppliers"),
        (Permission.PurchaseCreate, "Purchase", "Enter a purchase", "Record a supplier bill and take the goods in"),
        (Permission.PurchaseCancel, "Purchase", "Cancel a purchase", "Void a purchase entry and put the stock back"),
        (Permission.PurchaseReturn, "Purchase", "Send goods back", "Issue and cancel debit notes on a supplier"),

        (Permission.StockView, "Stock", "See stock", "Stock on hand, the ledger, and low-stock warnings"),
        (Permission.StockAdjust, "Stock", "Correct the shelf", "Change stock after a count. Always logged with their name"),
        (Permission.ProductManage, "Stock", "Manage the catalogue", "Add and edit parts, including their rates"),

        (Permission.PaymentRecord, "Money", "Take and make payments", "Receipts, supplier payments, cheques"),
        (Permission.PaymentCancel, "Money", "Cancel a payment", "Reverse a receipt or a payment already recorded"),
        (Permission.ExpenseRecord, "Money", "Record expenses", "Rent, salary, freight, shop expenses"),
        (Permission.CashDayClose, "Money", "Close the day", "Count the drawer and sign the day off"),
        (Permission.CapitalMovement, "Money", "Move money in and out",
            "Bank to till and back, capital the owner puts in, drawings taken out"),

        (Permission.CostView, "The numbers", "See cost and profit",
            "Buying price, margin, profit and loss, dead stock value. Most shops keep this off the counter"),
        (Permission.ReportView, "The numbers", "See registers and GST",
            "Every register and the GST return shapes — the shop's whole trade in one table"),

        (Permission.UserManage, "Admin", "Manage people and roles", "Add people, reset passwords, build roles"),
        (Permission.SettingsEdit, "Admin", "Change shop settings", "Name, GSTIN, address, invoice template"),
        (Permission.BooksLock, "Admin", "Lock and unlock the books", "Freeze a month that has been filed, or reopen it"),
        (Permission.AuditView, "Admin", "Read the audit trail", "Who cancelled, corrected or unlocked what, and when"),
    ];

    public static IReadOnlyList<PermissionDto> All() =>
        Entries.Select(e => new PermissionDto(e.Permission.ToString(), e.Label, e.Group, e.Description)).ToList();

    /// <summary>
    /// Parses names off the wire, quietly dropping anything unrecognised.
    /// <para>
    /// Dropping is right here: an unknown name is a permission this build does not enforce, and
    /// storing it would put a row in the database that looks like a grant and stops nothing.
    /// </para>
    /// </summary>
    public static IReadOnlyList<Permission> Parse(IEnumerable<string>? names) =>
        (names ?? [])
            .Select(name => Enum.TryParse<Permission>(name, ignoreCase: true, out var parsed)
                ? parsed
                : (Permission?)null)
            .Where(p => p is not null)
            .Select(p => p!.Value)
            .Distinct()
            .ToList();
}
