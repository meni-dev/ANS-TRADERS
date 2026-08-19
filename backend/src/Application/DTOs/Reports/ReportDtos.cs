namespace Application.DTOs.Reports;

/// <summary>
/// How a column should be read — by the screen when it aligns and formats, and by anyone reading
/// the exported file. Deliberately about meaning rather than about pixels.
/// </summary>
public enum RegisterCellType
{
    Text,
    Date,
    Money,
    Quantity,
    Number,
}

/// <summary>
/// <paramref name="Type"/> is the enum's name, not its number — the same convention every other DTO
/// here follows. Serialised as an integer it reads as a magic number in the browser and silently
/// stops matching the moment a member is inserted in the middle of the enum.
/// </summary>
public record RegisterColumn(string Key, string Label, string Type);

/// <summary>A figure printed under the table. Only columns worth adding up get one.</summary>
public record RegisterTotal(string ColumnKey, decimal Value);

/// <summary>
/// One register — the columns, the rows and what they add up to.
/// <para>
/// <b>Rows are lists of strings, not typed records.</b> Thirteen registers with a DTO each would
/// mean thirteen screens and thirteen export routines that drift apart; this way the server decides
/// what a register contains and the frontend has one table and one download for all of them. Numbers
/// travel as invariant decimal text (<c>"1234.50"</c>) so nothing is rounded on the way out — the
/// figure the CA opens is the figure in the database.
/// </para>
/// </summary>
public record RegisterDto(
    string Key,
    string Title,
    string Caption,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<RegisterColumn> Columns,
    IReadOnlyList<IReadOnlyList<string?>> Rows,
    IReadOnlyList<RegisterTotal> Totals,
    /// <summary>See <see cref="RegisterSummaryDto.IsAsAt"/>. The dates above are then today's.</summary>
    bool IsAsAt,
    /// <summary>
    /// Rows actually returned. Registers are not paged: a register that shows the CA half of March
    /// is worse than no register, and a shop's month is thousands of rows, not millions.
    /// </summary>
    int RowCount);

/// <summary>What the picker on the reports screen offers, so the list lives in one place.</summary>
/// <param name="IsAsAt">
/// True for the registers that describe a position rather than a period — what is on the shelf, who
/// owes what. Stock has one current level, not a level per date, so a date range means nothing to
/// them and the screen stops offering one.
/// </param>
public record RegisterSummaryDto(
    string Key, string Title, string Caption, string Group, bool IsAsAt = false);

public record RegisterQuery(string Key, DateOnly FromDate, DateOnly ToDate);
