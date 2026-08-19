using System.Globalization;
using Application.DTOs.Reports;

namespace Application.Common;

/// <summary>
/// Assembles a register a row at a time, keeping every row the same width as the column list.
/// <para>
/// The rows are positional, so a column added in one place and forgotten in another would silently
/// shift every figure one cell to the left — a purchase register where the CGST column shows the
/// SGST figure. {@link Row} takes the values by column key instead, and refuses anything it does
/// not recognise, so that shift cannot happen without an exception naming the column.
/// </para>
/// </summary>
public class RegisterBuilder
{
    private readonly List<RegisterColumn> _columns = [];
    private readonly List<IReadOnlyList<string?>> _rows = [];
    private readonly Dictionary<string, decimal> _totals = [];
    private readonly List<string> _totalled = [];

    public RegisterBuilder Text(string key, string label) => Column(key, label, RegisterCellType.Text);

    public RegisterBuilder Date(string key, string label) => Column(key, label, RegisterCellType.Date);

    public RegisterBuilder Number(string key, string label) => Column(key, label, RegisterCellType.Number);

    /// <param name="total">Adds a figure under the table. Only sums that mean something get one —
    /// a total of rates or of balances-after would read as money the shop does not have.</param>
    public RegisterBuilder Money(string key, string label, bool total = true)
    {
        if (total)
        {
            _totalled.Add(key);
            // Seeded at zero so a column that happens to have no figures still prints a total.
            // A missing total reads as "not calculated"; a zero reads as "nothing here".
            _totals[key] = 0m;
        }

        return Column(key, label, RegisterCellType.Money);
    }

    public RegisterBuilder Quantity(string key, string label, bool total = false)
    {
        if (total)
        {
            _totalled.Add(key);
            _totals[key] = 0m;
        }

        return Column(key, label, RegisterCellType.Quantity);
    }

    private RegisterBuilder Column(string key, string label, RegisterCellType type)
    {
        _columns.Add(new RegisterColumn(key, label, type.ToString()));
        return this;
    }

    public void Row(params (string Key, object? Value)[] cells)
    {
        var byKey = new Dictionary<string, object?>(cells.Length);

        foreach (var (key, value) in cells)
        {
            if (_columns.All(c => c.Key != key))
            {
                throw new InvalidOperationException($"Register has no column '{key}'");
            }

            byKey[key] = value;
        }

        var row = new string?[_columns.Count];

        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            byKey.TryGetValue(column.Key, out var value);
            row[i] = Render(value);

            if (value is decimal number && _totalled.Contains(column.Key))
            {
                _totals[column.Key] = _totals.GetValueOrDefault(column.Key) + number;
            }
        }

        _rows.Add(row);
    }

    /// <summary>
    /// Invariant culture throughout: a register opened in a spreadsheet set to a different locale
    /// must still read <c>1234.50</c> as a number, and <c>2026-08-19</c> as that day.
    /// </summary>
    private static string? Render(object? value) => value switch
    {
        null => null,
        decimal d => d.ToString("0.00##", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset moment => moment.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        int i => i.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "Yes" : "No",
        _ => value.ToString(),
    };

    public RegisterDto Build(
        string key, string title, string caption, DateOnly fromDate, DateOnly toDate, bool isAsAt = false) =>
        new(
            key,
            title,
            caption,
            fromDate,
            toDate,
            _columns,
            _rows,
            // Ordered by column so the figures under the table line up with the columns above it.
            _columns
                .Where(c => _totals.ContainsKey(c.Key))
                .Select(c => new RegisterTotal(c.Key, _totals[c.Key]))
                .ToList(),
            isAsAt,
            _rows.Count);
}
