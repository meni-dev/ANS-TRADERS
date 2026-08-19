using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class StockLedger : IStockLedger
{
    private readonly IStockRepository _repository;

    public StockLedger(IStockRepository repository)
    {
        _repository = repository;
    }

    public async Task RecordAsync(
        Product product,
        decimal signedQuantity,
        StockMovementType movementType,
        Guid? referenceId,
        string? referenceNumber,
        string? notes,
        CancellationToken cancellationToken,
        StockAdjustmentReason? adjustmentReason = null)
    {
        product.StockOnHand += signedQuantity;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.AddMovementAsync(
            new StockMovement
            {
                ProductId = product.Id,
                PartNumber = product.PartNumber,
                ItemName = product.ItemName,
                MovementType = movementType,
                Quantity = signedQuantity,
                BalanceAfter = product.StockOnHand,
                MovedAt = DateTimeOffset.UtcNow,
                ReferenceId = referenceId,
                ReferenceNumber = referenceNumber,
                AdjustmentReason = adjustmentReason,
            Notes = notes,
            },
            cancellationToken);
    }

    public void EnsureAvailable(Product product, decimal quantity, string action = "bill")
    {
        if (product.StockOnHand >= quantity)
        {
            return;
        }

        // Named quantities rather than a generic "insufficient stock": at a counter the useful
        // question is how many can go on the bill, and the answer is already known here.
        throw new ValidationAppException(new Dictionary<string, string[]>
        {
            ["Items"] =
            [
                $"Only {product.StockOnHand:0.###} {product.Uqc} of '{product.ItemName}' in stock — " +
                $"cannot {action} {quantity:0.###}",
            ],
        });
    }
}
