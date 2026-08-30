namespace Application.DTOs.Products;

public record UpdateProductRequest(
    string ItemCode,
    string PartNumber,
    string ItemName,
    string? Description,
    string? VehicleBrand,
    string? VehicleModel,
    string Hsn,
    decimal GstRate,
    /// <summary>Defaults to Taxable when not sent, which is what almost every part is.</summary>
    string? SupplyType,
    string Uqc,
    decimal PurchaseRate,
    decimal SellingRate,
    decimal Mrp,
    decimal ReorderLevel,
    bool IsActive);
