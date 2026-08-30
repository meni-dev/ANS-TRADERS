using Application.Common.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class StockLedger : IStockLedger
{
    private readonly IStockRepository _repository;
    private readonly IShopClock _clock;

    public StockLedger(IStockRepository repository, IShopClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task RecordAsync(
        Product product,
        decimal signedQuantity,
        StockMovementType movementType,
        DateOnly movementDate,
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
                MovementDate = movementDate,
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

    public async Task EnsureAvailableOnAsync(
        Product product,
        decimal quantity,
        DateOnly onDate,
        string action,
        CancellationToken cancellationToken)
    {
        if (onDate >= _clock.Today)
        {
            EnsureAvailable(product, quantity, action);
            return;
        }

        var held = await _repository.GetBalanceOnAsync(product.Id, onDate, cancellationToken);

        if (held >= quantity)
        {
            return;
        }

        // Both explanations, because it is genuinely one or the other and the counter is the only
        // one who can say which. Naming only the date would send somebody to change a date that was
        // right; naming only the purchase would send them looking for one that does not exist.
        throw new ValidationAppException(new Dictionary<string, string[]>
        {
            ["Items"] =
            [
                $"On {onDate:dd MMM yyyy} the shelf held {held:0.###} {product.Uqc} of " +
                $"'{product.ItemName}', so {quantity:0.###} cannot be {action}ed on that date. " +
                "Either the date is wrong, or the purchase that brought them in has not been " +
                "entered yet.",
            ],
        });
    }

    public void EnsureReversible(Product product, decimal quantity, string undoing, string remedy)
    {
        if (product.StockOnHand >= quantity)
        {
            return;
        }

        // A conflict rather than a validation error: nothing about the request is malformed. The
        // shop's own history has moved on, and the answer is a different document, not a corrected
        // field — so the message says which one.
        throw new ConflictException(
            $"Cancelling {undoing} would take {quantity:0.###} {product.Uqc} of '{product.ItemName}' " +
            $"back off the shelf, and only {product.StockOnHand:0.###} {product.Uqc} are left. " +
            $"The goods have already been sold on. {remedy}",
            "STOCK_WOULD_GO_NEGATIVE");
    }
}
