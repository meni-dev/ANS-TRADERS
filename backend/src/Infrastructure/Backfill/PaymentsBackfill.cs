using Application.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Backfill;

/// <summary>What the run did, printed for the owner before anything is trusted.</summary>
public record PaymentsBackfillReport(
    decimal ReceivableBefore,
    decimal ReceivableAfter,
    decimal OpeningBalancesSeeded,
    int CancelledDocumentsWithMoney,
    decimal MoneyOnCancelledDocuments,
    int PaymentsSynthesised,
    int LedgerEntriesWritten,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// The one assertion that catches almost every mistake in here: the only thing that may change
    /// the receivable is money the app could not previously see — the opening balances.
    /// </summary>
    public bool Reconciles =>
        Math.Round(ReceivableAfter - ReceivableBefore, 2) == Math.Round(OpeningBalancesSeeded, 2);
}

/// <summary>
/// Rebuilds the money history the app never recorded: a payment behind every tender that was only
/// ever a number on a document, and a party ledger seeded from those documents.
/// <para>
/// Deliberately a command rather than SQL inside a migration. It carries real decisions — how a
/// cancelled document is treated, which tender a credit bill was settled with — and those need to be
/// testable and, above all, <b>re-runnable once a mistake in them is found</b>. A migration runs once.
/// </para>
/// </summary>
public class PaymentsBackfill
{
    private readonly AppDbContext _context;

    public PaymentsBackfill(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentsBackfillReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();

        // The one and only window in which an append-only ledger may be truncated is before anybody
        // has written to it through the app. A standalone receipt means that window has closed:
        // SeedLedger rebuilds from documents and synthesised counter payments alone, so re-running
        // now would silently drop every real receipt, cheque bounce and hand adjustment, and leave
        // the party balances wrong with nothing on screen to say so.
        var liveReceipts = await _context.Payments
            .CountAsync(p => !p.IsCounterPayment, cancellationToken);

        if (liveReceipts > 0)
        {
            throw new InvalidOperationException(
                $"This database already holds {liveReceipts} receipt(s) recorded through the app. " +
                "The backfill rebuilds the party ledger from scratch and would discard them. It is " +
                "a one-time migration and has already served its purpose here.");
        }

        var receivableBefore = await _context.Invoices
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0)
            .SumAsync(i => (decimal?)i.BalanceDue, cancellationToken) ?? 0m;

        // Everything this command writes, it also owns. Clearing first is what makes a second run
        // produce the same database as the first — and this is the one and only window in which an
        // append-only ledger may be truncated, before anybody has read it.
        await ClearPreviousRunAsync(cancellationToken);

        var invoices = await _context.Invoices.OrderBy(i => i.InvoiceDate).ThenBy(i => i.Sequence)
            .ToListAsync(cancellationToken);

        var purchases = await _context.Purchases.OrderBy(p => p.InvoiceDate).ThenBy(p => p.Sequence)
            .ToListAsync(cancellationToken);

        var customers = await _context.Customers.ToListAsync(cancellationToken);
        var suppliers = await _context.Suppliers.ToListAsync(cancellationToken);

        var cancelledWithMoney = invoices
            .Where(i => i.Status == InvoiceStatus.Cancelled && i.AmountPaid > 0)
            .ToList();

        if (cancelledWithMoney.Count > 0)
        {
            // Worth saying out loud: these documents recorded money that was physically taken. It is
            // not being thrown away — each one still gets a payment row, marked reversed — but the
            // owner should see the list rather than find out later.
            warnings.Add(
                $"{cancelledWithMoney.Count} cancelled document(s) recorded " +
                $"{cancelledWithMoney.Sum(i => i.AmountPaid):0.00} of receipts. Each keeps a reversed " +
                "payment row, so the cash book still shows the money arriving and going back.");

            foreach (var invoice in cancelledWithMoney)
            {
                warnings.Add($"  {invoice.InvoiceNumber} — {invoice.AmountPaid:0.00} ({invoice.CustomerName})");
            }
        }

        var payments = new List<Payment>();

        payments.AddRange(SynthesiseInvoicePayments(invoices, customers));
        payments.AddRange(SynthesisePurchasePayments(purchases, suppliers));

        await _context.Payments.AddRangeAsync(payments, cancellationToken);

        // A cancelled document owes nothing, so nothing may count it as receivable. What it once
        // collected stays on AmountPaid and on the reversed payment above — between them the money
        // is still traceable, which it would not be if this zeroed the record to tidy it up.
        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Cancelled))
        {
            invoice.BalanceDue = 0;
        }

        foreach (var purchase in purchases.Where(p => p.Status == PurchaseStatus.Cancelled))
        {
            purchase.BalanceDue = 0;
        }

        var entries = SeedLedger(invoices, purchases, customers, suppliers, payments);
        await _context.PartyLedgerEntries.AddRangeAsync(entries, cancellationToken);

        // Derived from the ledger rather than copied from the opening balance, so the cached total
        // and the rows behind it cannot start out disagreeing.
        foreach (var customer in customers)
        {
            customer.OutstandingBalance = Round(
                entries.Where(e => e.CustomerId == customer.Id).Sum(e => e.Amount));
        }

        foreach (var supplier in suppliers)
        {
            supplier.OutstandingBalance = Round(
                entries.Where(e => e.SupplierId == supplier.Id).Sum(e => e.Amount));
        }

        await _context.SaveChangesAsync(cancellationToken);

        var receivableAfter =
            Round(customers.Where(c => c.OutstandingBalance > 0).Sum(c => c.OutstandingBalance))
            + Round(invoices
                .Where(i => i.CustomerId is null && i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0)
                .Sum(i => i.BalanceDue));

        return new PaymentsBackfillReport(
            Round(receivableBefore),
            Round(receivableAfter),
            Round(customers.Sum(c => c.OpeningBalance)),
            cancelledWithMoney.Count,
            Round(cancelledWithMoney.Sum(i => i.AmountPaid)),
            payments.Count,
            entries.Count,
            warnings);
    }

    private async Task ClearPreviousRunAsync(CancellationToken cancellationToken)
    {
        await _context.PartyLedgerEntries.ExecuteDeleteAsync(cancellationToken);

        // Only what this command created. A receipt somebody recorded through the app is real
        // history and must survive a re-run.
        await _context.PaymentAllocations
            .Where(a => a.Payment!.IsCounterPayment)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Payments
            .Where(p => p.IsCounterPayment)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static IEnumerable<Payment> SynthesiseInvoicePayments(
        IReadOnlyList<Invoice> invoices, IReadOnlyList<Customer> customers)
    {
        foreach (var invoice in invoices.Where(i => i.AmountPaid > 0))
        {
            var customer = customers.FirstOrDefault(c => c.Id == invoice.CustomerId);

            yield return BuildCounterPayment(
                PaymentDirection.Received,
                invoice.CustomerId,
                null,
                invoice.CustomerName,
                invoice.InvoiceDate,
                invoice.AmountPaid,
                invoice.PaymentMode,
                invoice.Id,
                invoice.InvoiceNumber,
                isInvoice: true,
                // A cancelled bill's tender came in and went straight back out. Recording it as
                // reversed keeps both halves of that visible.
                reversed: invoice.Status == InvoiceStatus.Cancelled,
                customerName: customer?.Name);
        }
    }

    private static IEnumerable<Payment> SynthesisePurchasePayments(
        IReadOnlyList<Purchase> purchases, IReadOnlyList<Supplier> suppliers)
    {
        foreach (var purchase in purchases.Where(p => p.AmountPaid > 0))
        {
            var supplier = suppliers.FirstOrDefault(s => s.Id == purchase.SupplierId);

            yield return BuildCounterPayment(
                PaymentDirection.Paid,
                null,
                purchase.SupplierId,
                purchase.SupplierName,
                purchase.InvoiceDate,
                purchase.AmountPaid,
                purchase.PaymentMode,
                purchase.Id,
                purchase.PurchaseNumber,
                isInvoice: false,
                reversed: purchase.Status == PurchaseStatus.Cancelled,
                customerName: supplier?.Name);
        }
    }

    private static Payment BuildCounterPayment(
        PaymentDirection direction,
        Guid? customerId,
        Guid? supplierId,
        string partyName,
        DateOnly date,
        decimal amount,
        PaymentMode documentMode,
        Guid documentId,
        string documentNumber,
        bool isInvoice,
        bool reversed,
        string? customerName)
    {
        var payment = new Payment
        {
            // No receipt number, historical or otherwise: the document the customer was handed is
            // their receipt, and minting a second number for one event is exactly what later
            // produces two pieces of paper for one transaction. It also leaves the standalone
            // receipt series genuinely gapless for the audit check.
            ReceiptNumber = null,
            Sequence = null,
            FinancialYear = FinancialYear.For(date),
            Direction = direction,
            PaymentDate = date,
            CustomerId = customerId,
            SupplierId = supplierId,
            PartyName = customerName ?? partyName,
            Amount = Round(amount),
            Mode = documentMode == PaymentMode.Credit ? PaymentMode.Cash : documentMode,
            IsCounterPayment = true,
            Status = reversed ? PaymentStatus.Reversed : PaymentStatus.Posted,
            Notes = reversed ? $"Collected on {documentNumber}, which was cancelled" : null,
        };

        payment.Allocations.Add(new PaymentAllocation
        {
            PaymentId = payment.Id,
            InvoiceId = isInvoice ? documentId : null,
            PurchaseId = isInvoice ? null : documentId,
            DocumentNumber = documentNumber,
            DocumentDate = date,
            Amount = payment.Amount,
            AllocatedAt = DateTimeOffset.UtcNow,
            IsReversed = reversed,
        });

        payment.AllocatedAmount = reversed ? 0m : payment.Amount;
        payment.UnallocatedAmount = reversed ? payment.Amount : 0m;

        return payment;
    }

    /// <summary>
    /// Replays every party's history in order and stamps a running balance, exactly as the live
    /// ledger would have if it had existed all along.
    /// </summary>
    private static List<PartyLedgerEntry> SeedLedger(
        IReadOnlyList<Invoice> invoices,
        IReadOnlyList<Purchase> purchases,
        IReadOnlyList<Customer> customers,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<Payment> payments)
    {
        var entries = new List<PartyLedgerEntry>();

        foreach (var customer in customers)
        {
            var events = new List<PendingEntry>();

            if (customer.OpeningBalance != 0)
            {
                events.Add(new PendingEntry(
                    DateOnly.FromDateTime(customer.CreatedAt.UtcDateTime),
                    SortKey: 0,
                    customer.OpeningBalance,
                    PartyLedgerEntryType.Opening,
                    null,
                    null,
                    "Opening balance"));
            }

            // Cancelled documents are skipped whole rather than written as an entry plus its
            // reversal. The net is identical and the statement stays readable — the same rule the
            // stock ledger's backfill follows.
            foreach (var invoice in invoices.Where(i =>
                         i.CustomerId == customer.Id && i.Status != InvoiceStatus.Cancelled))
            {
                events.Add(new PendingEntry(
                    invoice.InvoiceDate, 1, invoice.GrandTotal, PartyLedgerEntryType.Invoice,
                    invoice.Id, invoice.InvoiceNumber, null));
            }

            foreach (var payment in payments.Where(p =>
                         p.CustomerId == customer.Id && p.Status == PaymentStatus.Posted))
            {
                events.Add(new PendingEntry(
                    payment.PaymentDate, 2, -payment.Amount, PartyLedgerEntryType.PaymentReceived,
                    payment.Id, payment.Allocations.FirstOrDefault()?.DocumentNumber, null));
            }

            entries.AddRange(Materialise(events, customer.Id, null, customer.Name));
        }

        foreach (var supplier in suppliers)
        {
            var events = new List<PendingEntry>();

            if (supplier.OpeningBalance != 0)
            {
                events.Add(new PendingEntry(
                    DateOnly.FromDateTime(supplier.CreatedAt.UtcDateTime),
                    0, supplier.OpeningBalance, PartyLedgerEntryType.Opening, null, null, "Opening balance"));
            }

            foreach (var purchase in purchases.Where(p =>
                         p.SupplierId == supplier.Id && p.Status != PurchaseStatus.Cancelled))
            {
                events.Add(new PendingEntry(
                    purchase.InvoiceDate, 1, purchase.GrandTotal, PartyLedgerEntryType.PurchaseBill,
                    purchase.Id, purchase.PurchaseNumber, null));
            }

            foreach (var payment in payments.Where(p =>
                         p.SupplierId == supplier.Id && p.Status == PaymentStatus.Posted))
            {
                events.Add(new PendingEntry(
                    payment.PaymentDate, 2, -payment.Amount, PartyLedgerEntryType.PaymentMade,
                    payment.Id, payment.Allocations.FirstOrDefault()?.DocumentNumber, null));
            }

            entries.AddRange(Materialise(events, null, supplier.Id, supplier.Name));
        }

        return entries;
    }

    private static IEnumerable<PartyLedgerEntry> Materialise(
        List<PendingEntry> events, Guid? customerId, Guid? supplierId, string partyName)
    {
        var balance = 0m;
        var recordedAt = DateTimeOffset.UtcNow;

        // Ordered by date, then by what kind of event it was — never by id. A bill and the tender
        // taken against it share a date, and ids are random, so ordering by id would send the
        // running balance negative and then back, differently on every run.
        foreach (var pending in events.OrderBy(e => e.Date).ThenBy(e => e.SortKey))
        {
            balance = Round(balance + pending.Amount);

            yield return new PartyLedgerEntry
            {
                CustomerId = customerId,
                SupplierId = supplierId,
                PartyName = partyName,
                EntryType = pending.EntryType,
                Amount = Round(pending.Amount),
                BalanceAfter = balance,
                EntryDate = pending.Date,
                // Spaced so the statement's secondary sort reproduces the replay order rather than
                // collapsing every seeded row onto one instant.
                RecordedAt = recordedAt,
                ReferenceId = pending.ReferenceId,
                ReferenceNumber = pending.ReferenceNumber,
                Notes = pending.Notes,
            };

            recordedAt = recordedAt.AddMilliseconds(1);
        }
    }

    private record PendingEntry(
        DateOnly Date,
        int SortKey,
        decimal Amount,
        PartyLedgerEntryType EntryType,
        Guid? ReferenceId,
        string? ReferenceNumber,
        string? Notes);

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
