using Application.DTOs.Products;
using Domain.Entities;

namespace Application.Mapping;

public static class ProductMapper
{
    /// <param name="showCost">
    /// False for anyone without permission to see what the shop pays. The buying price is stripped
    /// here rather than in each screen, so a new screen cannot leak it by forgetting.
    /// </param>
    public static ProductDto ToDto(this Product product, bool showCost = true) => new(
        product.Id,
        product.ItemCode,
        product.PartNumber,
        product.ItemName,
        product.Description,
        product.VehicleBrand,
        product.VehicleModel,
        product.Hsn,
        product.GstRate,
        product.SupplyType.ToString(),
        product.CgstRate,
        product.SgstRate,
        product.Uqc,
        showCost ? product.PurchaseRate : null,
        product.SellingRate,
        product.Mrp,
        product.OpeningStock,
        product.StockOnHand,
        product.ReorderLevel,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt);
}
