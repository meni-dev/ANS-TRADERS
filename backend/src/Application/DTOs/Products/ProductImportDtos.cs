namespace Application.DTOs.Products;

/// <summary>
/// One row exactly as it came out of the spreadsheet.
/// <para>
/// <b>Every field is a string on purpose.</b> A catalogue file has whatever somebody typed —
/// <c>18%</c>, <c>₹ 1,250</c>, <c>eighteen</c>, an empty cell. Typed as <c>decimal</c> these would
/// fail JSON deserialisation and reject the <i>entire upload</i> with a message naming a property
/// path, so a 5,000-row file would die on one bad cell and the user would never learn which. As
/// strings, a bad cell is what it actually is: one row's validation error, reported next to its row
/// number.
/// </para>
/// </summary>
public record ProductImportRow(
    /// <summary>1-based, matching the spreadsheet with the header excluded — so the error report points at a row the user can see.</summary>
    int RowNumber,
    string? ItemCode,
    string? PartNumber,
    string? ItemName,
    string? Description,
    string? VehicleBrand,
    string? VehicleModel,
    string? Hsn,
    string? GstRate,
    string? Uqc,
    string? PurchaseRate,
    string? SellingRate,
    string? Mrp,
    string? OpeningStock,
    string? ReorderLevel);

public enum ImportRowAction
{
    Create = 0,
    Update = 1,
    Reject = 2,
}

public record ProductImportRowResult(
    int RowNumber,
    string PartNumber,
    string ItemName,
    string Action,
    IReadOnlyList<string> Errors);

/// <summary>What the file would do, before anything is written.</summary>
public record ProductImportPreviewDto(
    int TotalRows,
    int WillCreate,
    int WillUpdate,
    int Rejected,
    IReadOnlyList<ProductImportRowResult> Rows);

public record ProductImportRequest(
    IReadOnlyList<ProductImportRow> Rows,
    /// <summary>
    /// Off by default. On, a part number already on the master is updated rather than rejected —
    /// which is how a shop re-loads a supplier's revised price list.
    /// </summary>
    bool UpdateExisting);

public record ProductImportResultDto(int Created, int Updated);
