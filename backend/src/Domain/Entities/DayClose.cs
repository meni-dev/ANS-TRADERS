using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// One day's cash counted against what the app expected. The shop's own check on itself.
/// <para>
/// The figures are snapshotted rather than recomputed on read. A close is a statement about a
/// moment — "at 8pm on the 19th we counted ₹14,200 and the book said ₹14,850" — and if a backdated
/// receipt later changes what the book would say, the statement must not quietly change with it.
/// The difference that was found is the difference that was found.
/// </para>
/// </summary>
public class DayClose : AuditableEntity
{
    /// <summary>The business day being closed. One close per day — a unique index enforces it.</summary>
    public DateOnly CloseDate { get; set; }

    /// <summary>
    /// What was in the drawer when the day started — the previous close's <see cref="CountedCash"/>,
    /// not its expected figure. The drawer's truth is what somebody counted, not what the book hoped.
    /// </summary>
    public decimal OpeningCash { get; set; }

    public decimal CashReceived { get; set; }

    /// <summary>Cash paid to suppliers, and cash refunded to customers.</summary>
    public decimal CashPaidOut { get; set; }

    /// <summary>Rent, wages, tea — cash that left the drawer without a party behind it.</summary>
    public decimal CashExpenses { get; set; }

    /// <summary>
    /// The float, money drawn from the bank, capital put in — cash with no party behind it. Held
    /// apart from receipts because somebody reading a closed day needs to see that the till went up
    /// for a reason other than trade.
    /// </summary>
    public decimal CashOtherIn { get; set; }

    /// <summary>Money banked, and drawings taken out.</summary>
    public decimal CashOtherOut { get; set; }

    /// <summary>Opening + received − paid out − expenses. What should have been there.</summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>What was actually in the drawer.</summary>
    public decimal CountedCash { get; set; }

    /// <summary>
    /// Counted less expected. Negative is short, positive is over. Both need explaining — an
    /// unexplained surplus is as much a sign of a mis-keyed bill as a shortage is of a missing note.
    /// </summary>
    public decimal Difference { get; set; }

    /// <summary>Required whenever <see cref="Difference"/> is not zero.</summary>
    public string? Reason { get; set; }

    public string? Notes { get; set; }
}
