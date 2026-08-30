namespace Application.Common;

/// <summary>
/// The shop's own identity, bound from the <c>Business</c> configuration section. It supplies the
/// seller header printed on every invoice and — through <see cref="StateCode"/> — decides whether a
/// document is taxed as IGST or CGST+SGST.
/// </summary>
public class BusinessProfile
{
    public string Name { get; set; } = "ANS Traders";
    public string? LegalName { get; set; }
    public string? Gstin { get; set; }

    /// <summary>Two-digit GST state code of the place of business. Defaults to Tamil Nadu.</summary>
    public string StateCode { get; set; } = "33";

    public string State { get; set; } = "Tamil Nadu";

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>Free text printed at the foot of the bill, e.g. bank details or a returns policy.</summary>
    public string? InvoiceFooter { get; set; }
}
