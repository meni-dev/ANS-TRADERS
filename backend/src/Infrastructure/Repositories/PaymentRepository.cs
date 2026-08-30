using Application.DTOs.Payments;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <remarks>
    /// The documents behind the allocations come too, and they are not optional. Releasing an
    /// allocation puts money back on the bill it settled, which the ledger does through
    /// <c>allocation.Invoice</c> — an unloaded navigation there is silently null, so a cancel or a
    /// bounce would move the party's balance while leaving every bill still marked paid.
    /// </remarks>
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Payments
            .Include(p => p.Allocations).ThenInclude(a => a.Invoice)
            .Include(p => p.Allocations).ThenInclude(a => a.Purchase)
            .Include(p => p.Allocations).ThenInclude(a => a.CreditNote)
            .Include(p => p.Allocations).ThenInclude(a => a.DebitNote)
            .Include(p => p.Cheque)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchAsync(
        string? search,
        PaymentDirection? direction,
        PaymentStatus? status,
        PaymentMode? mode,
        Guid? customerId,
        Guid? supplierId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? unallocatedOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Include(p => p.Cheque)
            .AsQueryable();

        if (direction is { } d) query = query.Where(p => p.Direction == d);
        if (status is { } s) query = query.Where(p => p.Status == s);
        if (mode is { } m) query = query.Where(p => p.Mode == m);
        if (customerId is { } c) query = query.Where(p => p.CustomerId == c);
        if (supplierId is { } sup) query = query.Where(p => p.SupplierId == sup);
        if (fromDate is { } from) query = query.Where(p => p.PaymentDate >= from);
        if (toDate is { } to) query = query.Where(p => p.PaymentDate <= to);

        if (unallocatedOnly == true)
        {
            // A reversed payment keeps its figures as a record of what it did, so it would otherwise
            // show up as money still available to spend.
            query = query.Where(p => p.UnallocatedAmount > 0 && p.Status != PaymentStatus.Reversed);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                (p.ReceiptNumber != null && EF.Functions.ILike(p.ReceiptNumber, pattern)) ||
                EF.Functions.ILike(p.PartyName, pattern) ||
                (p.ReferenceNumber != null && EF.Functions.ILike(p.ReferenceNumber, pattern)) ||
                (p.Cheque != null && EF.Functions.ILike(p.Cheque.ChequeNumber, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first: the counter almost always wants the receipt just written.
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> SearchChequesAsync(
        ChequeStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Include(p => p.Cheque)
            .Where(p => p.Cheque != null);

        if (status is { } s) query = query.Where(p => p.Cheque!.Status == s);
        if (fromDate is { } from) query = query.Where(p => p.Cheque!.ChequeDate >= from);
        if (toDate is { } to) query = query.Where(p => p.Cheque!.ChequeDate <= to);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Soonest bankable first — the register is a to-do list, not a history.
            .OrderBy(p => p.Cheque!.ChequeDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForInvoiceAsync(
        Guid invoiceId, CancellationToken cancellationToken) =>
        await _context.PaymentAllocations
            // The payment comes along because releasing the money has to hand it back as an advance,
            // which moves the payment's own totals.
            .Include(a => a.Payment)
            .Where(a => a.InvoiceId == invoiceId && !a.IsReversed)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForCreditNoteAsync(
        Guid creditNoteId, CancellationToken cancellationToken) =>
        await _context.PaymentAllocations
            .Include(a => a.Payment)
            .Where(a => a.CreditNoteId == creditNoteId && !a.IsReversed)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForDebitNoteAsync(
        Guid debitNoteId, CancellationToken cancellationToken) =>
        await _context.PaymentAllocations
            .Include(a => a.Payment)
            .Where(a => a.DebitNoteId == debitNoteId && !a.IsReversed)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentAllocation>> GetLiveAllocationsForPurchaseAsync(
        Guid purchaseId, CancellationToken cancellationToken) =>
        await _context.PaymentAllocations
            .Include(a => a.Payment)
            .Where(a => a.PurchaseId == purchaseId && !a.IsReversed)
            .ToListAsync(cancellationToken);

    /// <remarks>
    /// Deliberately tracked, unlike the paged searches: the allocation path mutates these documents,
    /// and a no-tracking query would let the writes disappear silently.
    /// </remarks>
    public async Task<IReadOnlyList<Invoice>> GetOpenInvoicesForCustomerAsync(
        Guid customerId, CancellationToken cancellationToken) =>
        await _context.Invoices
            .Where(i => i.CustomerId == customerId
                        && i.Status != InvoiceStatus.Cancelled
                        && i.BalanceDue > 0)
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Purchase>> GetOpenPurchasesForSupplierAsync(
        Guid supplierId, CancellationToken cancellationToken) =>
        await _context.Purchases
            .Where(p => p.SupplierId == supplierId
                        && p.Status != PurchaseStatus.Cancelled
                        && p.BalanceDue > 0)
            .OrderBy(p => p.InvoiceDate)
            .ThenBy(p => p.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpenDocumentDto>> GetOpenDocumentsAsync(
        Guid? customerId, Guid? supplierId, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (customerId is { } customer)
        {
            var rows = await _context.Invoices
                .AsNoTracking()
                .Where(i => i.CustomerId == customer
                            && i.Status != InvoiceStatus.Cancelled
                            && i.BalanceDue > 0)
                .OrderBy(i => i.InvoiceDate)
                .ThenBy(i => i.Sequence)
                .Select(i => new
                {
                    i.Id, i.InvoiceNumber, i.InvoiceDate, i.DueDate,
                    i.GrandTotal, i.AmountPaid, i.BalanceDue,
                })
                .ToListAsync(cancellationToken);

            // Age runs from the due date where one exists, so a customer on 30-day terms is not
            // reported as a month late on the day his credit period starts.
            return rows.Select(r => new OpenDocumentDto(
                r.Id, r.InvoiceNumber, r.InvoiceDate, r.DueDate,
                r.GrandTotal, r.AmountPaid, r.BalanceDue,
                asOf.DayNumber - (r.DueDate ?? r.InvoiceDate).DayNumber)).ToList();
        }

        var bills = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId
                        && p.Status != PurchaseStatus.Cancelled
                        && p.BalanceDue > 0)
            .OrderBy(p => p.InvoiceDate)
            .ThenBy(p => p.Sequence)
            .Select(p => new
            {
                p.Id, p.PurchaseNumber, p.InvoiceDate,
                p.GrandTotal, p.AmountPaid, p.BalanceDue,
            })
            .ToListAsync(cancellationToken);

        // Suppliers carry no due date — PaymentTerms is deliberately free text, so there is nothing
        // to compute one from.
        return bills.Select(b => new OpenDocumentDto(
            b.Id, b.PurchaseNumber, b.InvoiceDate, null,
            b.GrandTotal, b.AmountPaid, b.BalanceDue,
            asOf.DayNumber - b.InvoiceDate.DayNumber)).ToList();
    }

    public async Task<CustomerAccountSummaryDto?> GetCustomerAccountSummaryAsync(
        Guid customerId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.OutstandingBalance, c.CreditLimit, c.CreditDays })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null) return null;

        var openInvoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId
                        && i.Status != InvoiceStatus.Cancelled
                        && i.BalanceDue > 0)
            .Select(i => new { i.BalanceDue, Due = i.DueDate ?? i.InvoiceDate })
            .ToListAsync(cancellationToken);

        // Sixty days past due, not sixty days old. This is the figure the warning is built on, and
        // it is the one that actually predicts a bad debt.
        var overdueCutoff = asOf.AddDays(-60);

        var advance = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId
                        && p.Status == PaymentStatus.Posted
                        && p.UnallocatedAmount > 0)
            .SumAsync(p => (decimal?)p.UnallocatedAmount, cancellationToken) ?? 0m;

        // Money promised on paper but not yet in the bank. Shown apart from the advance so the
        // counter never reads a cheque in the drawer as cash on account.
        var pendingCheques = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId
                        && p.Cheque != null
                        && (p.Cheque.Status == ChequeStatus.Pending
                            || p.Cheque.Status == ChequeStatus.Deposited))
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        // A bounce older than three months says nothing useful about today's customer.
        var lastBounce = await _context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId
                        && p.Cheque != null
                        && p.Cheque.Status == ChequeStatus.Bounced
                        && p.Cheque.BouncedOn >= asOf.AddDays(-90))
            .OrderByDescending(p => p.Cheque!.BouncedOn)
            .Select(p => new { p.Cheque!.BouncedOn, p.Cheque.ChequeNumber })
            .FirstOrDefaultAsync(cancellationToken);

        return new CustomerAccountSummaryDto(
            customerId,
            Round(customer.OutstandingBalance),
            Round(customer.CreditLimit),
            customer.CreditDays,
            Round(advance),
            Round(pendingCheques),
            Round(openInvoices.Where(i => i.Due < overdueCutoff).Sum(i => i.BalanceDue)),
            openInvoices.Count == 0 ? null : openInvoices.Min(i => i.Due),
            lastBounce?.BouncedOn,
            lastBounce?.ChequeNumber);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken) =>
        await _context.Payments.AddAsync(payment, cancellationToken);

    public void AddAllocation(PaymentAllocation allocation) =>
        _context.PaymentAllocations.Add(allocation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
