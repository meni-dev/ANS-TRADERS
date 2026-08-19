namespace Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string ItemCode,
    string PartNumber,
    string ItemName,
    string? Description,
    string? VehicleBrand,
    string? VehicleModel,
    string Hsn,
    decimal GstRate,
    decimal CgstRate,
    decimal SgstRate,
    string Uqc,
    /// <summary>
    /// Null when the caller may not see cost. Not zero — zero would read as a part the shop gets
    /// free, and a margin report built on it would be nonsense. Null says <i>not shown to you</i>.
    /// </summary>
    decimal? PurchaseRate,
    decimal SellingRate,
    decimal Mrp,
    decimal OpeningStock,
    decimal StockOnHand,
    decimal ReorderLevel,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
