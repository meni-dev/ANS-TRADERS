using Application.Common.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace UnitTests;

/// <summary>Collects the movements a test produces instead of touching a database.</summary>
internal sealed class FakeStockRepository : IStockRepository
{
    public List<StockMovement> Movements { get; } = [];

    public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        Movements.Add(movement);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchStockAsync(
        string? search, bool? lowOnly, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<(int TotalItems, int LowStockCount, int OutOfStockCount, decimal TotalStockValue)>
        GetStockSummaryAsync(string? search, bool? lowOnly, bool? activeOnly, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> SearchMovementsAsync(
        string? search, Guid? productId, StockMovementType? movementType, DateOnly? fromDate, DateOnly? toDate,
        int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<(Domain.Enums.StockAdjustmentReason Reason, decimal Quantity, decimal Value)>>
        GetAdjustmentsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ProductShelfFacts>> GetShelfFactsAsync(
        DateOnly asOf, int velocityWindowDays, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class StockLedgerTests
{
    private static Product Part(decimal stockOnHand) => new()
    {
        PartNumber = "BP001",
        ItemName = "Brake Pad - Front",
        Uqc = "PCS",
        StockOnHand = stockOnHand,
    };

    [Fact]
    public async Task RecordAsync_MovesStockAndStampsTheResultingBalance()
    {
        var repository = new FakeStockRepository();
        var ledger = new StockLedger(repository);
        var product = Part(20);

        await ledger.RecordAsync(
            product, 12, StockMovementType.Purchase, Guid.NewGuid(), "PUR/2026-27/0001", null, default);

        Assert.Equal(32, product.StockOnHand);

        var movement = Assert.Single(repository.Movements);
        Assert.Equal(12, movement.Quantity);
        Assert.Equal(32, movement.BalanceAfter);
        Assert.Equal("PUR/2026-27/0001", movement.ReferenceNumber);
        // Snapshotted, so the ledger still reads correctly after the part is renamed.
        Assert.Equal("Brake Pad - Front", movement.ItemName);
    }

    [Fact]
    public async Task RecordAsync_TakesStockOutOnANegativeQuantity()
    {
        var repository = new FakeStockRepository();
        var ledger = new StockLedger(repository);
        var product = Part(20);

        await ledger.RecordAsync(product, -5, StockMovementType.Sale, null, "INV/2026-27/0001", null, default);

        Assert.Equal(15, product.StockOnHand);
        Assert.Equal(15, repository.Movements[0].BalanceAfter);
    }

    [Fact]
    public async Task RecordAsync_RunsTheBalanceForwardAcrossMovements()
    {
        var repository = new FakeStockRepository();
        var ledger = new StockLedger(repository);
        var product = Part(0);

        await ledger.RecordAsync(product, 10, StockMovementType.Opening, null, null, "Opening stock", default);
        await ledger.RecordAsync(product, 25, StockMovementType.Purchase, null, "PUR/1", null, default);
        await ledger.RecordAsync(product, -4, StockMovementType.Sale, null, "INV/1", null, default);

        Assert.Equal([10m, 35m, 31m], repository.Movements.Select(m => m.BalanceAfter));
        Assert.Equal(31, product.StockOnHand);
    }

    [Fact]
    public void EnsureAvailable_PassesWhenStockCoversTheQuantity()
    {
        var ledger = new StockLedger(new FakeStockRepository());

        ledger.EnsureAvailable(Part(5), 5);
    }

    [Fact]
    public void EnsureAvailable_RejectsBillingMoreThanIsOnTheShelf()
    {
        var ledger = new StockLedger(new FakeStockRepository());

        var exception = Assert.Throws<ValidationAppException>(() => ledger.EnsureAvailable(Part(3), 4));

        // The message names what is available, because that is the number the counter needs next.
        Assert.Contains("Only 3", exception.Errors["Items"][0]);
        Assert.Contains("Brake Pad - Front", exception.Errors["Items"][0]);
    }

    [Fact]
    public void EnsureAvailable_RejectsAnythingWhenTheItemIsOutOfStock()
    {
        var ledger = new StockLedger(new FakeStockRepository());

        Assert.Throws<ValidationAppException>(() => ledger.EnsureAvailable(Part(0), 1));
    }
}
