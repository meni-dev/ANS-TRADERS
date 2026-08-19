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
    string Uqc,
    decimal PurchaseRate,
    decimal SellingRate,
    decimal Mrp,
    decimal ReorderLevel,
    bool IsActive);
