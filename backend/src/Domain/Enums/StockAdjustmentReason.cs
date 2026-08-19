namespace Domain.Enums;

/// <summary>
/// Why stock was corrected by hand. Coded rather than free text, because the question a shop
/// actually asks is "how much did I lose to damage this year" — and a sentence cannot be added up.
/// <para>
/// The free-text note stays alongside it. The code is for counting; the note is for explaining.
/// </para>
/// </summary>
public enum StockAdjustmentReason
{
    /// <summary>
    /// The count was simply wrong before — a miscount, a bill entered twice, a part put on the
    /// wrong shelf. No money was lost; the book was.
    /// </summary>
    CountingError = 0,

    /// <summary>Broken in handling, rusted, packaging destroyed. Real money, gone.</summary>
    Damage = 1,

    /// <summary>Past its shelf life — oils, sealants, batteries.</summary>
    Expiry = 2,

    /// <summary>Missing with no explanation. Worth knowing separately from damage.</summary>
    TheftOrMissing = 3,

    /// <summary>Given away — a goodwill replacement, a sample, a warranty fit.</summary>
    FreeIssue = 4,

    /// <summary>Sent to a supplier or scrap dealer outside the normal return path.</summary>
    Scrapped = 5,

    Other = 6,
}
