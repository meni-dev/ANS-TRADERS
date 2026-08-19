using Application.DTOs.Dashboard;
using Domain.Entities;

namespace Application.Mapping;

public static class DashboardMapper
{
    /// <summary>
    /// Projects a product onto the dashboard's reorder row. The stock screen's own DTO carries
    /// pricing and value columns the dashboard has no room for.
    /// </summary>
    public static ReorderItemDto ToReorderDto(this Product product) => new(
        product.Id,
        product.PartNumber,
        product.ItemName,
        product.Uqc,
        product.StockOnHand,
        product.ReorderLevel);
}
