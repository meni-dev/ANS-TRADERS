using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;

namespace UnitTests;

/// <summary>
/// Holds a party's open invoices in memory. Only the members the ledger actually reaches for are
/// implemented; the rest throw, so a test that starts depending on one fails loudly rather than
/// quietly passing against a stub that returns nothing.
/// </summary>
/// <summary>
/// Counts up per series, the way the real one does, without needing a database to do it.
/// </summary>
internal sealed class CountingNumbers : IDocumentNumbers
{
    private readonly Dictionary<(DocumentKind, string), int> _last = [];

    public Task<int> NextAsync(DocumentKind kind, string financialYear, CancellationToken cancellationToken)
    {
        var key = (kind, financialYear);
        _last[key] = _last.GetValueOrDefault(key) + 1;
        return Task.FromResult(_last[key]);
    }
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    public List<Invoice> OpenInvoices { get; } = [];
    public List<Payment> Added { get; } = [];
    public List<PaymentAllocation> StagedAllocations { get; } = [];

    public Task<IReadOnlyList<Invoice>> GetOpenInvoicesForCustomerAsync(
        Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            OpenInvoices
                .Where(i => i.CustomerId == customerId && i.BalanceDue > 0)
                .OrderBy(i => i.InvoiceDate)
                .ThenBy(i => i.Sequence)
                .ToList());

    public Task<IReadOnlyList<Purchase>> GetOpenPurchasesForSupplierAsync(
        Guid supplierId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Purchase>>([]);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        Added.Add(payment);
        return Task.CompletedTask;
    }

    public void AddAllocation(PaymentAllocation allocation) => StagedAllocations.Add(allocation);


    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

    public Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchAsync(
        string? search, PaymentDirection? direction, PaymentStatus? status, PaymentMode? mode,
        Guid? customerId, Guid? supplierId, DateOnly? fromDate, DateOnly? toDate,
        bool? unallocatedOnly, int page, int pageSize, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchChequesAsync(
        ChequeStatus? status, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize,
        CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForInvoiceAsync(
        Guid invoiceId, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForPurchaseAsync(
        Guid purchaseId, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForCreditNoteAsync(
        Guid creditNoteId, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForDebitNoteAsync(
        Guid debitNoteId, CancellationToken ct) => throw new NotSupportedException();

    public Task<IReadOnlyList<Application.DTOs.Payments.OpenDocumentDto>> GetOpenDocumentsAsync(
        Guid? customerId, Guid? supplierId, DateOnly asOf, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<Application.DTOs.Payments.CustomerAccountSummaryDto?> GetCustomerAccountSummaryAsync(
        Guid customerId, DateOnly asOf, CancellationToken ct) => throw new NotSupportedException();
}

public class PaymentLedgerTests
{
    private static readonly DateOnly Today = new(2026, 8, 18);

    private static (PaymentLedger Ledger, FakePaymentRepository Payments, FakePartyLedgerRepository Entries)
        Build()
    {
        var payments = new FakePaymentRepository();
        var entries = new FakePartyLedgerRepository();
        return (new PaymentLedger(payments, new PartyLedger(entries), new CountingNumbers()), payments, entries);
    }

    private static Customer ACustomer() => new() { Name = "Kumar Motors", Phone = "9791122334" };

    private static Invoice AnOpenInvoice(Customer customer, string number, DateOnly date, decimal total)
        => new()
        {
            InvoiceNumber = number,
            InvoiceDate = date,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            GrandTotal = total,
            AmountPaid = 0,
            BalanceDue = total,
        };

    private static PaymentDraft ChequeDraftFor(
        Customer customer, decimal amount, DateOnly chequeDate) =>
        new(PaymentDirection.Received, customer, null, customer.Name, Today, amount,
            PaymentMode.Cheque, null, null, false,
            new ChequeDraft("445102", "HDFC", chequeDate, Today), []);

    [Fact]
    public async Task PostDatedChequeSettlesNothingUntilItIsBanked()
    {
        var (ledger, repository, entries) = Build();
        var customer = ACustomer();
        var invoice = AnOpenInvoice(customer, "INV/2026-27/0001", new(2026, 8, 1), 5_000m);
        repository.OpenInvoices.Add(invoice);

        // Dated a month out — real paper, but not money the shop can use. The caller still names the
        // bill it is meant for, because that is what PaymentService computes for every payment; the
        // point of the test is that a pending payment must ignore it rather than reserve it.
        var payment = await ledger.ReceiveAsync(
            ChequeDraftFor(customer, 2_000m, Today.AddDays(30)) with
            {
                Allocations = [new AllocationTarget(invoice, null, 2_000m)],
            },
            CancellationToken.None);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Empty(payment.Allocations);
        Assert.Equal(2_000m, payment.UnallocatedAmount);

        // The bill has not moved, and neither has the customer's balance.
        Assert.Equal(5_000m, invoice.BalanceDue);
        Assert.Equal(0m, invoice.AmountPaid);
        Assert.Equal(0m, customer.OutstandingBalance);
        Assert.Empty(entries.Entries);
    }

    /// <remarks>
    /// Allocating at receipt instead would let two post-dated cheques each reserve the same bill —
    /// both would see the full balance still outstanding — and overpay it when they were banked.
    /// </remarks>
    [Fact]
    public async Task BankingAPostDatedChequeSettlesWhateverIsOpenOnThatDay()
    {
        var (ledger, repository, entries) = Build();
        var customer = ACustomer();
        var older = AnOpenInvoice(customer, "INV/2026-27/0001", new(2026, 7, 1), 1_200m);
        var newer = AnOpenInvoice(customer, "INV/2026-27/0002", new(2026, 8, 1), 5_000m);
        repository.OpenInvoices.AddRange([older, newer]);

        // Aimed at the newer bill on the day it was taken. A month later the older one is still
        // open, and that is what the money should actually settle.
        var payment = await ledger.ReceiveAsync(
            ChequeDraftFor(customer, 2_000m, Today.AddDays(30)) with
            {
                Allocations = [new AllocationTarget(newer, null, 2_000m)],
            },
            CancellationToken.None);

        payment.Customer = customer;
        var bankedOn = Today.AddDays(30);
        await ledger.PostAsync(payment, bankedOn, CancellationToken.None);

        Assert.Equal(PaymentStatus.Posted, payment.Status);

        // Oldest first, and the effective date moves to the day it reached the bank so the money
        // lands in the month it actually arrived.
        Assert.Equal(bankedOn, payment.PaymentDate);
        Assert.Equal(0m, older.BalanceDue);
        Assert.Equal(4_200m, newer.BalanceDue);
        Assert.Equal(2_000m, payment.AllocatedAmount);
        Assert.Equal(0m, payment.UnallocatedAmount);

        // Only now does the balance move, and it moves by the whole cheque.
        var entry = Assert.Single(entries.Entries);
        Assert.Equal(PartyLedgerEntryType.PaymentReceived, entry.EntryType);
        Assert.Equal(-2_000m, entry.Amount);
        Assert.Equal(bankedOn, entry.EntryDate);
    }

    [Fact]
    public async Task AllocationsAreStagedExplicitlySoTheySurviveOnAnAlreadyTrackedPayment()
    {
        var (ledger, repository, _) = Build();
        var customer = ACustomer();
        repository.OpenInvoices.Add(
            AnOpenInvoice(customer, "INV/2026-27/0001", new(2026, 7, 1), 1_200m));

        var payment = await ledger.ReceiveAsync(
            ChequeDraftFor(customer, 800m, Today.AddDays(30)), CancellationToken.None);

        payment.Customer = customer;
        await ledger.PostAsync(payment, Today.AddDays(30), CancellationToken.None);

        // Entity hands out its Id in the initialiser, so EF reads a row added to a tracked parent as
        // an UPDATE against a row that does not exist. The repository must be told about it.
        Assert.Single(repository.StagedAllocations);
        Assert.Equal(payment.Id, repository.StagedAllocations[0].PaymentId);
    }

    [Fact]
    public async Task ACurrentDatedChequeSettlesImmediately()
    {
        var (ledger, repository, entries) = Build();
        var customer = ACustomer();
        var invoice = AnOpenInvoice(customer, "INV/2026-27/0001", new(2026, 8, 1), 5_000m);
        repository.OpenInvoices.Add(invoice);

        // A cheque the shop could walk to the bank today is money; one 30 days out is a promise.
        var payment = await ledger.ReceiveAsync(
            ChequeDraftFor(customer, 2_000m, Today) with
            {
                Allocations = [new AllocationTarget(invoice, null, 2_000m)],
            },
            CancellationToken.None);

        Assert.Equal(PaymentStatus.Posted, payment.Status);
        Assert.Equal(3_000m, invoice.BalanceDue);
        Assert.Single(entries.Entries);
    }

    private static Supplier ASupplier() => new() { Name = "Sundaram Spares", Phone = "9840011223" };

    private static PaymentDraft CashDraft(
        Customer? customer, Supplier? supplier, PaymentDirection direction, decimal amount) =>
        new(direction, customer, supplier, customer?.Name ?? supplier!.Name, Today, amount,
            PaymentMode.Cash, null, null, false, null, []);

    /// <remarks>
    /// The two combinations that existed before refunds were legal. They are pinned because the sign
    /// is now derived rather than hard-coded, and a derivation that quietly changed either of them
    /// would rewrite balances across the whole shop.
    /// </remarks>
    [Fact]
    public async Task MoneyInFromACustomerReducesWhatTheyOwe()
    {
        var (ledger, _, entries) = Build();
        var customer = ACustomer();

        await ledger.ReceiveAsync(
            CashDraft(customer, null, PaymentDirection.Received, 1_000m), CancellationToken.None);

        Assert.Equal(-1_000m, Assert.Single(entries.Entries).Amount);
        Assert.Equal(-1_000m, customer.OutstandingBalance);
    }

    [Fact]
    public async Task MoneyOutToASupplierReducesWhatWeOweThem()
    {
        var (ledger, _, entries) = Build();
        var supplier = ASupplier();

        await ledger.ReceiveAsync(
            CashDraft(null, supplier, PaymentDirection.Paid, 1_000m), CancellationToken.None);

        Assert.Equal(-1_000m, Assert.Single(entries.Entries).Amount);
        Assert.Equal(-1_000m, supplier.OutstandingBalance);
    }

    /// <remarks>
    /// The combination the old hard-coded sign got backwards. A customer holding a 1,000 advance is
    /// at −1,000; handing the cash back must bring them to zero, not to −2,000.
    /// </remarks>
    [Fact]
    public async Task RefundingACustomerBringsTheirCreditBackToZero()
    {
        var (ledger, _, entries) = Build();
        var customer = ACustomer();

        await ledger.ReceiveAsync(
            CashDraft(customer, null, PaymentDirection.Received, 1_000m), CancellationToken.None);
        Assert.Equal(-1_000m, customer.OutstandingBalance);

        await ledger.ReceiveAsync(
            CashDraft(customer, null, PaymentDirection.Paid, 1_000m), CancellationToken.None);

        Assert.Equal(1_000m, entries.Entries[^1].Amount);
        Assert.Equal(0m, customer.OutstandingBalance);
    }

    [Fact]
    public async Task ReversingAPaymentPutsTheBalanceBackExactly()
    {
        var (ledger, _, _) = Build();
        var customer = ACustomer();

        var payment = await ledger.ReceiveAsync(
            CashDraft(customer, null, PaymentDirection.Received, 1_000m), CancellationToken.None);

        payment.Customer = customer;
        await ledger.ReverseAsync(
            payment, PartyLedgerEntryType.PaymentCancelled, Today, "keyed wrong", CancellationToken.None);

        Assert.Equal(0m, customer.OutstandingBalance);
    }

    [Fact]
    public async Task ReversingASupplierPaymentPutsTheBalanceBackExactly()
    {
        var (ledger, _, _) = Build();
        var supplier = ASupplier();

        var payment = await ledger.ReceiveAsync(
            CashDraft(null, supplier, PaymentDirection.Paid, 1_000m), CancellationToken.None);

        payment.Supplier = supplier;
        await ledger.ReverseAsync(
            payment, PartyLedgerEntryType.PaymentCancelled, Today, "keyed wrong", CancellationToken.None);

        Assert.Equal(0m, supplier.OutstandingBalance);
    }
}
