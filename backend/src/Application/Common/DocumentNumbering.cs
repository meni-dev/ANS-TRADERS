namespace Application.Common;

/// <summary>
/// Rules about a document series as a whole. A gap in the numbering is the first thing an auditor
/// looks for, so what counts as one is business logic and lives here rather than in a query.
/// </summary>
public static class DocumentNumbering
{
    /// <summary>
    /// Sequence numbers absent from an otherwise continuous run.
    /// <para>
    /// Counted from 1, not from the lowest number present: a series that starts at 4 is missing its
    /// first three documents, not merely offset. Cancelled documents keep their number, so a gap
    /// always means a row that was never written.
    /// </para>
    /// </summary>
    public static IReadOnlyList<int> FindGaps(IEnumerable<int> sequences)
    {
        var present = sequences.ToHashSet();

        if (present.Count == 0)
        {
            return [];
        }

        return Enumerable.Range(1, present.Max()).Where(n => !present.Contains(n)).ToList();
    }
}
