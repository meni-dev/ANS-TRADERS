namespace Application.Common;

/// <summary>
/// Indian financial years run 1 April to 31 March, and document numbering restarts with each one.
/// Both purchase and invoice numbering depend on this, so the rule lives in one place.
/// </summary>
public static class FinancialYear
{
    private const int StartMonth = 4;

    /// <summary>Formats a date's financial year as <c>2026-27</c>.</summary>
    public static string For(DateOnly date)
    {
        var startYear = date.Month >= StartMonth ? date.Year : date.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:D2}";
    }
}
