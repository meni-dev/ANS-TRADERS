namespace Domain.Enums;

/// <summary>
/// Two roles, not a permission matrix. A shop with four people does not need thirty checkboxes, and
/// a matrix nobody configures correctly protects less than a rule everybody understands.
/// </summary>
public enum UserRole
{
    /// <summary>Works the counter: bills, receipts, returns, stock.</summary>
    Staff = 0,

    /// <summary>
    /// Everything Staff can do, plus the things that rewrite history or hide it — cancelling
    /// documents, adjusting stock, locking the books, and managing who else can sign in.
    /// </summary>
    Owner = 1,
}
