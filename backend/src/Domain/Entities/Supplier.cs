using Domain.Common;

namespace Domain.Entities;

public class Supplier : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Natural business key, matching how <see cref="Customer"/> is identified.</summary>
    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>15-character GSTIN. Needed to claim input tax credit on purchases.</summary>
    public string? Gstin { get; set; }

    public string? ContactPerson { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    /// <summary>
    /// Two-digit GST state code. Purchases compare this against the buyer's state to decide
    /// between CGST+SGST and IGST.
    /// </summary>
    public string? StateCode { get; set; }

    public string? Pincode { get; set; }

    /// <summary>Free text, e.g. "30 days" or "Advance".</summary>
    public string? PaymentTerms { get; set; }

    /// <summary>Outstanding carried in when the supplier starts being tracked. Set once, at creation.</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>What the shop owes this supplier right now. See <see cref="Customer.OutstandingBalance"/>.</summary>
    public decimal OutstandingBalance { get; set; }

    public bool IsActive { get; set; } = true;
}
