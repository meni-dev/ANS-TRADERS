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

    public Task<decimal> GetBalanceOnAsync(Guid productId, DateOnly onDate, CancellationToken cancellationToken) =>
        Task.FromResult(Movements
            .Where(m => m.ProductId == productId && m.MovementDate <= onDate)
            .Sum(m => m.Quantity));

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
    private static StockLedger Ledger(FakeStockRepository repository, DateOnly? today = null) =>
        new(repository, new FixedClock(today ?? new DateOnly(2026, 8, 27)));

    private static DateOnly On(int day) => new(2026, 8, day);

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
        var ledger = Ledger(repository);
        var product = Part(20);

        await ledger.RecordAsync(
            product, 12, StockMovementType.Purchase, On(5), Guid.NewGuid(), "PUR/2026-27/0001", null,
            default);

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
        var ledger = Ledger(repository);
        var product = Part(20);

        await ledger.RecordAsync(
            product, -5, StockMovementType.Sale, On(5), null, "INV/2026-27/0001", null, default);

        Assert.Equal(15, product.StockOnHand);
        Assert.Equal(15, repository.Movements[0].BalanceAfter);
    }

    [Fact]
    public async Task RecordAsync_RunsTheBalanceForwardAcrossMovements()
    {
        var repository = new FakeStockRepository();
        var ledger = Ledger(repository);
        var product = Part(0);

        await ledger.RecordAsync(product, 10, StockMovementType.Opening, On(1), null, null, "Opening stock", default);
        await ledger.RecordAsync(product, 25, StockMovementType.Purchase, On(2), null, "PUR/1", null, default);
        await ledger.RecordAsync(product, -4, StockMovementType.Sale, On(3), null, "INV/1", null, default);

        Assert.Equal([10m, 35m, 31m], repository.Movements.Select(m => m.BalanceAfter));
        Assert.Equal(31, product.StockOnHand);
    }

    [Fact]
    public void EnsureAvailable_PassesWhenStockCoversTheQuantity()
    {
        var ledger = Ledger(new FakeStockRepository());

        ledger.EnsureAvailable(Part(5), 5);
    }

    [Fact]
    public void EnsureAvailable_RejectsBillingMoreThanIsOnTheShelf()
    {
        var ledger = Ledger(new FakeStockRepository());

        var exception = Assert.Throws<ValidationAppException>(() => ledger.EnsureAvailable(Part(3), 4));

        // The message names what is available, because that is the number the counter needs next.
        Assert.Contains("Only 3", exception.Errors["Items"][0]);
        Assert.Contains("Brake Pad - Front", exception.Errors["Items"][0]);
    }

    [Fact]
    public void EnsureAvailable_RejectsAnythingWhenTheItemIsOutOfStock()
    {
        var ledger = Ledger(new FakeStockRepository());

        Assert.Throws<ValidationAppException>(() => ledger.EnsureAvailable(Part(0), 1));
    }

    /// <summary>
    /// Undoing a purchase or a credit note takes stock back off the shelf. If the goods have since
    /// been sold on there is nothing there to take, and letting the reversal through leaves a
    /// negative quantity — which then values the stock at a negative number and puts the shelf out
    /// of step with its own ledger.
    /// </summary>
    [Fact]
    public void A_reversal_larger_than_what_is_left_on_the_shelf_is_refused()
    {
        var ledger = Ledger(new FakeStockRepository());
        var product = new Product { ItemName = "Clutch Plate", Uqc = "PCS", StockOnHand = 0 };

        var error = Assert.Throws<ConflictException>(() =>
            ledger.EnsureReversible(product, 5, "PUR/2026-27/0006", "Raise a debit note instead."));

        Assert.Contains("PUR/2026-27/0006", error.Message);
        Assert.Contains("Raise a debit note instead.", error.Message);
    }

    [Fact]
    public void A_reversal_the_shelf_can_cover_goes_through()
    {
        var ledger = Ledger(new FakeStockRepository());
        var product = new Product { ItemName = "Clutch Plate", Uqc = "PCS", StockOnHand = 5 };

        ledger.EnsureReversible(product, 5, "PUR/2026-27/0006", "Raise a debit note instead.");
    }

    /// <summary>
    /// The boundary: exactly enough is enough. Refusing here would block the ordinary case of a
    /// purchase entered twice and one copy cancelled before anything was sold.
    /// </summary>
    [Fact]
    public void A_reversal_of_exactly_what_is_left_goes_through()
    {
        var ledger = Ledger(new FakeStockRepository());
        var product = new Product { ItemName = "Clutch Plate", Uqc = "PCS", StockOnHand = 2.5m };

        ledger.EnsureReversible(product, 2.5m, "CRN/2026-27/0004", "Raise a fresh bill instead.");
    }

    /// <summary>
    /// A bill dated today asks the cheap question — the shelf as it is now. This is the case that
    /// runs a hundred times a day and it must not pay for a query.
    /// </summary>
    [Fact]
    public async Task A_document_dated_today_is_checked_against_todays_shelf()
    {
        var repository = new FakeStockRepository();
        var ledger = Ledger(repository, new DateOnly(2026, 8, 27));
        var product = Part(4);

        await ledger.EnsureAvailableOnAsync(product, 4, new DateOnly(2026, 8, 27), "bill", default);

        var tooMany = await Assert.ThrowsAsync<ValidationAppException>(() =>
            ledger.EnsureAvailableOnAsync(product, 5, new DateOnly(2026, 8, 27), "bill", default));
        Assert.Contains("in stock", tooMany.Errors["Items"][0]);
    }

    /// <summary>
    /// The one this was built for. Ten arrive on the 12th, and a bill is back-dated to the 5th —
    /// when the shelf was empty. Today's stock says yes; the day it claims to have happened says no.
    /// </summary>
    [Fact]
    public async Task A_back_dated_document_is_checked_against_the_shelf_as_it_stood()
    {
        var repository = new FakeStockRepository();
        var ledger = Ledger(repository, new DateOnly(2026, 8, 27));
        var product = Part(0);

        await ledger.RecordAsync(
            product, 10, StockMovementType.Purchase, new DateOnly(2026, 8, 12), null, "PUR/1", null, default);

        var error = await Assert.ThrowsAsync<ValidationAppException>(() =>
            ledger.EnsureAvailableOnAsync(product, 5, new DateOnly(2026, 8, 5), "bill", default));

        var message = error.Errors["Items"][0];
        Assert.Contains("05 Aug 2026", message);
        Assert.Contains("the purchase that brought them in has not been entered yet", message);
    }

    [Fact]
    public async Task A_back_dated_document_after_the_goods_arrived_goes_through()
    {
        var repository = new FakeStockRepository();
        var ledger = Ledger(repository, new DateOnly(2026, 8, 27));
        var product = Part(0);

        await ledger.RecordAsync(
            product, 10, StockMovementType.Purchase, new DateOnly(2026, 8, 12), null, "PUR/1", null, default);

        await ledger.EnsureAvailableOnAsync(product, 5, new DateOnly(2026, 8, 20), "bill", default);
    }

    /// <summary>
    /// The movement carries the document's date, not the day the row was written — that is the
    /// column's whole reason for existing.
    /// </summary>
    [Fact]
    public async Task A_movement_is_stamped_with_its_documents_date()
    {
        var repository = new FakeStockRepository();
        var ledger = Ledger(repository);
        var product = Part(20);

        await ledger.RecordAsync(
            product, -3, StockMovementType.Sale, new DateOnly(2026, 8, 5), null, "INV/1", null, default);

        var movement = Assert.Single(repository.Movements);
        Assert.Equal(new DateOnly(2026, 8, 5), movement.MovementDate);
        Assert.NotEqual(default, movement.MovedAt);
    }
}
