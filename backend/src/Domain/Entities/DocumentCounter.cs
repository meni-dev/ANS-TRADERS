using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// The last number handed out in one series, for one financial year.
/// <para>
/// A row per series rather than a database sequence: sequences do not roll back, so a failed bill
/// would burn its number and leave a hole in a series an auditor reads as a missing document. This
/// is claimed inside the caller's transaction, so a document that is not saved does not consume a
/// number.
/// </para>
/// </summary>
public class DocumentCounter : Entity
{
    public DocumentKind Kind { get; set; }

    public string FinancialYear { get; set; } = string.Empty;

    /// <summary>The highest number handed out so far. The next document takes this plus one.</summary>
    public int LastNumber { get; set; }
}
