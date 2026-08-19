using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// The shop's own identity and preferences — one row, always. Previously this lived in
/// <c>appsettings.json</c>, which meant a shopkeeper could not correct their own address without a
/// developer and a restart.
/// <para>
/// <see cref="StateCode"/> decides whether a document is taxed as IGST or CGST+SGST. Changing it
/// does not rewrite history: every invoice and purchase snapshots <c>IsInterState</c> at creation.
/// </para>
/// </summary>
public class ShopSettings : AuditableEntity
{
    /// <summary>
    /// The single row's identity, fixed so the row can be found without a "first or default" scan
    /// and can never be accidentally duplicated.
    /// </summary>
    public static readonly Guid SingletonId = new("5e771a6c-0000-4000-8000-000000000001");

    public string Name { get; set; } = "ANS Traders";

    /// <summary>Registered name, when it differs from the trading name.</summary>
    public string? LegalName { get; set; }

    public string? Gstin { get; set; }

    /// <summary>Two-digit GST state code of the place of business.</summary>
    public string StateCode { get; set; } = "33";

    public string State { get; set; } = "Tamil Nadu";

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Free text printed at the foot of the bill — a returns policy, a jurisdiction note.</summary>
    public string? InvoiceFooter { get; set; }

    /// <summary>Printed by the templates that carry a payment block. Free text, so any format works.</summary>
    public string? BankDetails { get; set; }

    /// <summary>Terms and conditions, printed by the templates that have room for them.</summary>
    public string? InvoiceTerms { get; set; }

    public InvoiceTemplate InvoiceTemplate { get; set; } = InvoiceTemplate.Classic;

    /// <summary>
    /// Nothing dated on or before this may be written, cancelled or adjusted.
    /// <para>
    /// Set after a GST return is filed. Once the shop has told the department what a month contained,
    /// changing that month makes the filing wrong — and the change is silent, so nobody finds out
    /// until a notice arrives. Only the owner can move it, and moving it is logged.
    /// </para>
    /// </summary>
    public DateOnly? BooksLockedUpTo { get; set; }
}
