namespace Domain.Enums;

/// <summary>
/// Which layout the printed bill uses. Every one of these is a complete GST tax invoice — they
/// differ in density, emphasis and styling, never in which legally required field appears. A
/// template that dropped the GSTIN or the tax split would not be a valid document.
/// </summary>
public enum InvoiceTemplate
{
    /// <summary>Clean A4 with the tax split as table columns. The default.</summary>
    Classic = 0,

    /// <summary>A4 plus a rate-wise tax summary, bank details and terms — what an accountant wants.</summary>
    Detailed = 1,

    /// <summary>A4 with a colour accent band and a stronger typographic hierarchy.</summary>
    Modern = 2,

    /// <summary>A4 fully ruled in boxes, the way a local press prints an invoice book.</summary>
    Traditional = 3,

    /// <summary>A4 stripped to hairline rules and white space.</summary>
    Minimal = 4,
}
