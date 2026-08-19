using Domain.Common;

namespace Domain.Entities;

public class Customer : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Natural business key. A counter shop looks a customer up by phone, not by name.</summary>
    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>15-character GSTIN. Absent for unregistered walk-in customers.</summary>
    public string? Gstin { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    /// <summary>
    /// Two-digit GST state code. Billing compares this against the seller's state to decide
    /// between CGST+SGST and IGST, so it is stored rather than derived from the state name.
    /// </summary>
    public string? StateCode { get; set; }

    public string? Pincode { get; set; }

    public decimal CreditLimit { get; set; }

    /// <summary>Outstanding carried in when the customer starts being tracked. Set once, at creation.</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// What this customer owes right now. A running total of <see cref="PartyLedgerEntry"/>,
    /// denormalised here for the same reason <see cref="Product.StockOnHand"/> is — the billing
    /// screen reads it on every customer selection.
    /// <para>Negative means an advance is held. That is legal; never clamp it to zero.</para>
    /// </summary>
    public decimal OutstandingBalance { get; set; }

    /// <summary>
    /// Days of credit allowed, used to default an invoice's due date. Zero means no terms agreed,
    /// in which case the bill is due on issue.
    /// </summary>
    public int CreditDays { get; set; }

    public bool IsActive { get; set; } = true;
}
