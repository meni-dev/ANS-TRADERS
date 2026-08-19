using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Money the shop spent on running itself — rent, salary, electricity — as opposed to money paid to
/// a supplier for goods.
/// <para>
/// A standalone document rather than a <see cref="Payment"/> with no party. A payment settles
/// something a party is owed and carries allocations, a receipt number and a party balance; an
/// expense settles nothing and belongs to nobody. Forcing it through the payment path would put
/// rent in the middle of the supplier settlement list and give it a receipt number it has no use
/// for. The cash book unions the two, which is the only place they genuinely belong together.
/// </para>
/// <para>
/// Without these the shop's "profit" is revenue less cost of goods, which for a business paying
/// rent and wages is not profit at all.
/// </para>
/// </summary>
public class Expense : AuditableEntity
{
    /// <summary>Running number, e.g. <c>EXP/2026-27/0001</c>. Its own series.</summary>
    public string ExpenseNumber { get; set; } = string.Empty;

    public string FinancialYear { get; set; } = string.Empty;

    public int Sequence { get; set; }

    /// <summary>The day the money went out, which is the day it belongs to in the accounts.</summary>
    public DateOnly ExpenseDate { get; set; }

    public ExpenseCategory Category { get; set; }

    /// <summary>Always positive. It is an expense; the direction is in the document type.</summary>
    public decimal Amount { get; set; }

    /// <summary>How it was paid. Cash and UPI move the drawer; a transfer does not.</summary>
    public PaymentMode Mode { get; set; } = PaymentMode.Cash;

    /// <summary>Cheque number, UPI reference, transfer number — whatever proves it.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Who it was paid to, in plain words. Not a party — the landlord is not a supplier.</summary>
    public string? PaidTo { get; set; }

    public string? Notes { get; set; }

    /// <summary>Cancelled expenses keep their number and their row, like every other document.</summary>
    public bool IsCancelled { get; set; }
}
