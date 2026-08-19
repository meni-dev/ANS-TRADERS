using Application.DTOs.Stock;
using Domain.Entities;

namespace Application.Mapping;

public static class StockMapper
{
    public static ProductStockDto ToStockDto(this Product product) => new(
        product.Id,
        product.PartNumber,
        product.ItemName,
        product.VehicleBrand,
        product.VehicleModel,
        product.Uqc,
        product.OpeningStock,
        product.StockOnHand,
        product.ReorderLevel,
        // Valued at cost, not selling price: this is what the shop has tied up in the shelf, and
        // valuing unsold stock at what it might fetch overstates it.
        Math.Round(product.StockOnHand * product.PurchaseRate, 2, MidpointRounding.AwayFromZero),
        product.IsActive);

    public static StockMovementDto ToDto(this StockMovement movement) => new(
        movement.Id,
        movement.ProductId,
        movement.PartNumber,
        movement.ItemName,
        movement.MovementType.ToString(),
        movement.Quantity,
        movement.BalanceAfter,
        movement.MovedAt,
        movement.ReferenceId,
        movement.ReferenceNumber,
        movement.Notes);
}
