using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace UnitTests;

/// <summary>Collects entries instead of touching a database, mirroring <c>FakeStockRepository</c>.</summary>
internal sealed class FakePartyLedgerRepository : IPartyLedgerRepository
{
    public List<PartyLedgerEntry> Entries { get; } = [];

    public Task AddEntryAsync(PartyLedgerEntry entry, CancellationToken cancellationToken)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<PartyLedgerEntry> Items, int TotalCount, decimal OpeningBalance,
            decimal RangeMovement, decimal CarriedIn)>
        GetStatementAsync(
            Guid? customerId, Guid? supplierId, DateOnly? fromDate, DateOnly? toDate,
            int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<decimal> SumForPartyAsync(
        Guid? customerId, Guid? supplierId, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.Sum(e => e.Amount));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class PartyLedgerTests
{
    private static readonly DateOnly Today = new(2026, 8, 18);

    private static (PartyLedger Ledger, FakePartyLedgerRepository Repository) Build()
    {
        var repository = new FakePartyLedgerRepository();
        return (new PartyLedger(repository), repository);
    }

    private static Customer Ramesh(decimal opening = 0) =>
        new() { Name = "Ramesh Auto", Phone = "9840012345", OutstandingBalance = opening };

    [Fact]
    public async Task RecordForCustomer_RaisingABillIncreasesWhatIsOwed()
    {
        var (ledger, repository) = Build();
        var customer = Ramesh();

        await ledger.RecordForCustomerAsync(
            customer, 12_000, PartyLedgerEntryType.Invoice, Today, null, "INV/2026-27/0412", null, default);

        Assert.Equal(12_000, customer.OutstandingBalance);

        var entry = Assert.Single(repository.Entries);
        Assert.Equal(12_000, entry.Amount);
        Assert.Equal(12_000, entry.BalanceAfter);
        Assert.Equal(customer.Id, entry.CustomerId);
        // Snapshotted, so the statement still reads correctly after a rename.
        Assert.Equal("Ramesh Auto", entry.PartyName);
    }

    [Fact]
    public async Task RecordForCustomer_ReceiptReducesWhatIsOwed()
    {
        var (ledger, repository) = Build();
        var customer = Ramesh();

        await ledger.RecordForCustomerAsync(
            customer, 12_000, PartyLedgerEntryType.Invoice, Today, null, null, null, default);
        await ledger.RecordForCustomerAsync(
            customer, -12_000, PartyLedgerEntryType.PaymentReceived, Today, null, null, null, default);

        Assert.Equal(0, customer.OutstandingBalance);
        Assert.Equal([12_000m, 0m], repository.Entries.Select(e => e.BalanceAfter));
    }

    /// <summary>
    /// Paying more than is owed leaves an advance. Clamping at zero would lose money the shop is
    /// genuinely holding.
    /// </summary>
    [Fact]
    public async Task RecordForCustomer_AllowsTheBalanceToGoNegative()
    {
        var (ledger, _) = Build();
        var customer = Ramesh();

        await ledger.RecordForCustomerAsync(
            customer, 1_000, PartyLedgerEntryType.Invoice, Today, null, null, null, default);
        await ledger.RecordForCustomerAsync(
            customer, -5_000, PartyLedgerEntryType.PaymentReceived, Today, null, null, null, default);

        Assert.Equal(-4_000, customer.OutstandingBalance);
    }

    /// <summary>
    /// The three lines a bounce must leave behind: bill, receipt, and the receipt coming back —
    /// a balance that silently reappears causes an argument at the counter.
    /// </summary>
    [Fact]
    public async Task RecordForCustomer_ABouncedChequeRestoresTheBalanceAndLeavesAThirdLine()
    {
        var (ledger, repository) = Build();
        var customer = Ramesh();

        await ledger.RecordForCustomerAsync(
            customer, 12_000, PartyLedgerEntryType.Invoice, Today, null, null, null, default);
        await ledger.RecordForCustomerAsync(
            customer, -12_000, PartyLedgerEntryType.PaymentReceived, Today, null, null, null, default);
        await ledger.RecordForCustomerAsync(
            customer, 12_000, PartyLedgerEntryType.ChequeBounced, Today, null, null, "Funds insufficient", default);

        Assert.Equal(12_000, customer.OutstandingBalance);
        Assert.Equal(3, repository.Entries.Count);

        var bounce = repository.Entries[2];
        Assert.Equal(PartyLedgerEntryType.ChequeBounced, bounce.EntryType);
        Assert.Equal("Funds insufficient", bounce.Notes);
    }

    [Fact]
    public async Task RecordForCustomer_CarriesTheOpeningBalanceForward()
    {
        var (ledger, repository) = Build();
        var customer = Ramesh(opening: 4_300);

        await ledger.RecordForCustomerAsync(
            customer, 1_700, PartyLedgerEntryType.Invoice, Today, null, null, null, default);

        Assert.Equal(6_000, customer.OutstandingBalance);
        Assert.Equal(6_000, Assert.Single(repository.Entries).BalanceAfter);
    }

    [Fact]
    public async Task RecordForSupplier_IsTheSameShapeInTheOtherDirection()
    {
        var (ledger, repository) = Build();
        var supplier = new Supplier { Name = "Bosch Distributors", Phone = "9012345678" };

        await ledger.RecordForSupplierAsync(
            supplier, 8_000, PartyLedgerEntryType.PurchaseBill, Today, null, "PUR/2026-27/0007", null, default);
        await ledger.RecordForSupplierAsync(
            supplier, -3_000, PartyLedgerEntryType.PaymentMade, Today, null, null, null, default);

        // Positive still means "open on this account" — here, what the shop owes.
        Assert.Equal(5_000, supplier.OutstandingBalance);

        var entry = repository.Entries[0];
        Assert.Equal(supplier.Id, entry.SupplierId);
        Assert.Null(entry.CustomerId);
    }

    /// <summary>
    /// The invariant the whole design rests on: the denormalised balance is always the sum of the
    /// ledger, and the last row's running total agrees with it.
    /// </summary>
    [Fact]
    public async Task Balance_AlwaysEqualsTheSumOfTheLedger()
    {
        var (ledger, repository) = Build();
        var customer = Ramesh(opening: 2_500);

        var script = new (decimal Amount, PartyLedgerEntryType Type)[]
        {
            (2_500, PartyLedgerEntryType.Opening),
            (12_000, PartyLedgerEntryType.Invoice),
            (-12_000, PartyLedgerEntryType.PaymentReceived),
            (12_000, PartyLedgerEntryType.ChequeBounced),
            (4_000, PartyLedgerEntryType.Invoice),
            (-6_000, PartyLedgerEntryType.PaymentReceived),
            (-4_000, PartyLedgerEntryType.InvoiceCancelled),
            (150, PartyLedgerEntryType.ChequeBounceCharge),
            (-50, PartyLedgerEntryType.Adjustment),
        };

        // The opening row is the balance the customer was created with, so it is not added twice.
        foreach (var (amount, type) in script.Skip(1))
        {
            await ledger.RecordForCustomerAsync(
                customer, amount, type, Today, null, null, null, default);
        }

        var ledgerSum = repository.Entries.Sum(e => e.Amount) + 2_500;

        Assert.Equal(ledgerSum, customer.OutstandingBalance);
        Assert.Equal(customer.OutstandingBalance, repository.Entries[^1].BalanceAfter);
    }

    [Fact]
    public async Task RecordForCustomer_RoundsToPaise()
    {
        var (ledger, _) = Build();
        var customer = Ramesh();

        await ledger.RecordForCustomerAsync(
            customer, 33.333m, PartyLedgerEntryType.Invoice, Today, null, null, null, default);

        Assert.Equal(33.33m, customer.OutstandingBalance);
    }

    [Fact]
    public async Task RecordForCustomer_StampsBothTheBusinessDateAndTheSystemTime()
    {
        var (ledger, repository) = Build();
        var backDated = new DateOnly(2026, 7, 1);

        await ledger.RecordForCustomerAsync(
            Ramesh(), 500, PartyLedgerEntryType.Invoice, backDated, null, null, null, default);

        var entry = Assert.Single(repository.Entries);

        // A day's collections filter on the business date; the statement orders by system time, so
        // a back-dated row still sorts where it was entered.
        Assert.Equal(backDated, entry.EntryDate);
        Assert.True(entry.RecordedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}
