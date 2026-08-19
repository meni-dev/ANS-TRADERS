using Application.Common;
using Application.DTOs.Dashboard;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardTodayDto> GetDayTotalsAsync(
        DateOnly date, CancellationToken cancellationToken)
    {
        var (sales, invoiceCount, purchases, purchaseCount) =
            await GetTotalsAsync(date, date, cancellationToken);

        return new DashboardTodayDto(sales, invoiceCount, purchases, purchaseCount);
    }

    public async Task<(decimal SalesTotal, int InvoiceCount, decimal PurchaseTotal)>
        GetRangeTotalsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var (sales, invoiceCount, purchases, _) =
            await GetTotalsAsync(fromDate, toDate, cancellationToken);

        return (sales, invoiceCount, purchases);
    }

    public async Task<MoneyPositionDto> GetMoneyPositionAsync(
        DateOnly asOf, CancellationToken cancellationToken)
    {
        // Ageing is expressed as date cut-offs rather than a day count, because EF cannot translate
        // DateOnly arithmetic into SQL — and comparing against a constant keeps the index usable.
        var thirtyDays = asOf.AddDays(-30);
        var sixtyDays = asOf.AddDays(-60);

        var openInvoices = _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0);

        // Buckets are cut on the due date, falling back to the invoice date where no terms were
        // given. The fallback is what keeps every historical row behaving exactly as it used to.
        var ageing = await openInvoices
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                NotDue = g.Sum(i => (i.DueDate ?? i.InvoiceDate) > asOf ? i.BalanceDue : 0m),
                Current = g.Sum(i =>
                    (i.DueDate ?? i.InvoiceDate) <= asOf && (i.DueDate ?? i.InvoiceDate) >= thirtyDays
                        ? i.BalanceDue
                        : 0m),
                Days31To60 = g.Sum(i =>
                    (i.DueDate ?? i.InvoiceDate) < thirtyDays && (i.DueDate ?? i.InvoiceDate) >= sixtyDays
                        ? i.BalanceDue
                        : 0m),
                Over60 = g.Sum(i => (i.DueDate ?? i.InvoiceDate) < sixtyDays ? i.BalanceDue : 0m),
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Term one of the receivable: what the accounts say, which is the only figure that also
        // knows about advances taken and charges raised with no bill behind them.
        var partyReceivable = await _context.Customers
            .AsNoTracking()
            .Where(c => c.OutstandingBalance > 0)
            .SumAsync(c => (decimal?)c.OutstandingBalance, cancellationToken) ?? 0m;

        // Term two: credit given to somebody with no account. Provably disjoint from term one —
        // term one never reads an invoice, and this excludes every invoice that has a customer.
        var walkInReceivable = await openInvoices
            .Where(i => i.CustomerId == null)
            .SumAsync(i => (decimal?)i.BalanceDue, cancellationToken) ?? 0m;

        // Filtered on > 0 above, so this has to be counted separately rather than netted: one
        // customer sitting in credit must not cancel out another customer's debt.
        var advancesHeld = await _context.Customers
            .AsNoTracking()
            .Where(c => c.OutstandingBalance < 0)
            .SumAsync(c => (decimal?)c.OutstandingBalance, cancellationToken) ?? 0m;

        var customersWithDues = await _context.Customers
            .AsNoTracking()
            .CountAsync(c => c.OutstandingBalance > 0, cancellationToken);

        var payable = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.OutstandingBalance > 0)
            .SumAsync(s => (decimal?)s.OutstandingBalance, cancellationToken) ?? 0m;

        var suppliersWithDues = await _context.Suppliers
            .AsNoTracking()
            .CountAsync(s => s.OutstandingBalance > 0, cancellationToken);

        var payableBillCount = await _context.Purchases
            .AsNoTracking()
            .CountAsync(p => p.Status != PurchaseStatus.Cancelled && p.BalanceDue > 0, cancellationToken);

        var receivable = Round(partyReceivable + walkInReceivable);

        // The buckets are cut from invoices, but the headline comes from accounts, and the two
        // differ by whatever sits on a party's ledger with no bill behind it — a bounce charge, a
        // hand-written adjustment, an advance already applied. Left alone, the parts of the tile
        // would not add up to its total, which is the fastest way to make somebody stop trusting it.
        //
        // The remainder goes into the newest bucket. That is the conservative direction: it can only
        // ever understate how overdue the money is, and "overdue" is the figure that makes the shop
        // pick up the phone.
        var aged = Round((ageing?.NotDue ?? 0m) + (ageing?.Current ?? 0m)
            + (ageing?.Days31To60 ?? 0m) + (ageing?.Over60 ?? 0m));

        return new MoneyPositionDto(
            receivable,
            ageing?.Count ?? 0,
            customersWithDues,
            Round(ageing?.NotDue ?? 0m),
            Round((ageing?.Current ?? 0m) + (receivable - aged)),
            Round(ageing?.Days31To60 ?? 0m),
            Round(ageing?.Over60 ?? 0m),
            Round(payable),
            payableBillCount,
            suppliersWithDues,
            // Stored negative, shown as a positive amount held.
            Round(-advancesHeld));
    }

    public async Task<GstSummaryDto> GetGstSummaryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var output = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled
                        && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Taxable = g.Sum(i => i.TaxableAmount),
                Cgst = g.Sum(i => i.CgstAmount),
                Sgst = g.Sum(i => i.SgstAmount),
                Igst = g.Sum(i => i.IgstAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var input = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.Status != PurchaseStatus.Cancelled
                        && p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Taxable = g.Sum(p => p.TaxableAmount),
                Cgst = g.Sum(p => p.CgstAmount),
                Sgst = g.Sum(p => p.SgstAmount),
                Igst = g.Sum(p => p.IgstAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Goods that came back were never really supplied, so the tax on them is not tax the shop
        // owes. Without this the shop pays GST on a sale it reversed — a direct, silent loss on
        // every return.
        var creditNotes = await _context.CreditNotes
            .AsNoTracking()
            .Where(n => n.Status != CreditNoteStatus.Cancelled
                        && n.NoteDate >= fromDate && n.NoteDate <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Taxable = g.Sum(n => n.TaxableAmount),
                Cgst = g.Sum(n => n.CgstAmount),
                Sgst = g.Sum(n => n.SgstAmount),
                Igst = g.Sum(n => n.IgstAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        // The mirror: goods sent back to a supplier take their input credit with them.
        var debitNotes = await _context.DebitNotes
            .AsNoTracking()
            .Where(n => n.Status != DebitNoteStatus.Cancelled
                        && n.NoteDate >= fromDate && n.NoteDate <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Taxable = g.Sum(n => n.TaxableAmount),
                Cgst = g.Sum(n => n.CgstAmount),
                Sgst = g.Sum(n => n.SgstAmount),
                Igst = g.Sum(n => n.IgstAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        // GSTR-1 Table 12 is reported per HSN and unit, off the sales side only.
        //
        // The join is flattened into an anonymous row before grouping: EF cannot translate a group
        // whose elements are still the join's transparent identifier, and projecting straight into a
        // positional record on top of that fails the same way.
        var hsnRows = await SalesLines(fromDate, toDate)
            .GroupBy(x => new { x.Hsn, x.Uqc })
            .Select(g => new
            {
                g.Key.Hsn,
                g.Key.Uqc,
                Quantity = g.Sum(i => i.Quantity),
                TaxableValue = g.Sum(i => i.TaxableAmount),
                Cgst = g.Sum(i => i.CgstAmount),
                Sgst = g.Sum(i => i.SgstAmount),
                Igst = g.Sum(i => i.IgstAmount),
            })
            .OrderByDescending(r => r.TaxableValue)
            .ToListAsync(cancellationToken);

        // Netted per HSN as well. Table 12 carries quantity and taxable value, and a return that
        // showed only in the totals would leave those rows claiming goods that came back.
        var returnRows = await ReturnLines(fromDate, toDate)
            .GroupBy(x => new { x.Hsn, x.Uqc })
            .Select(g => new
            {
                g.Key.Hsn,
                g.Key.Uqc,
                Quantity = g.Sum(i => i.Quantity),
                TaxableValue = g.Sum(i => i.TaxableAmount),
                Cgst = g.Sum(i => i.CgstAmount),
                Sgst = g.Sum(i => i.SgstAmount),
                Igst = g.Sum(i => i.IgstAmount),
            })
            .ToListAsync(cancellationToken);

        var returnsByKey = returnRows.ToDictionary(r => (r.Hsn, r.Uqc));

        var hsn = hsnRows
            .Select(r =>
            {
                var back = returnsByKey.GetValueOrDefault((r.Hsn, r.Uqc));

                return new
                {
                    r.Hsn,
                    r.Uqc,
                    Quantity = r.Quantity - (back?.Quantity ?? 0m),
                    TaxableValue = r.TaxableValue - (back?.TaxableValue ?? 0m),
                    Cgst = r.Cgst - (back?.Cgst ?? 0m),
                    Sgst = r.Sgst - (back?.Sgst ?? 0m),
                    Igst = r.Igst - (back?.Igst ?? 0m),
                };
            })
            .Select(r => new HsnSummaryRowDto(
                r.Hsn,
                r.Uqc,
                r.Quantity,
                Round(r.TaxableValue),
                Round(r.Cgst),
                Round(r.Sgst),
                Round(r.Igst),
                Round(r.Cgst + r.Sgst + r.Igst)))
            .ToList();

        var outputCgst = Round((output?.Cgst ?? 0m) - (creditNotes?.Cgst ?? 0m));
        var outputSgst = Round((output?.Sgst ?? 0m) - (creditNotes?.Sgst ?? 0m));
        var outputIgst = Round((output?.Igst ?? 0m) - (creditNotes?.Igst ?? 0m));
        var outputTotal = outputCgst + outputSgst + outputIgst;

        var inputCgst = Round((input?.Cgst ?? 0m) - (debitNotes?.Cgst ?? 0m));
        var inputSgst = Round((input?.Sgst ?? 0m) - (debitNotes?.Sgst ?? 0m));
        var inputIgst = Round((input?.Igst ?? 0m) - (debitNotes?.Igst ?? 0m));
        var inputTotal = inputCgst + inputSgst + inputIgst;

        return new GstSummaryDto(
            Round((output?.Taxable ?? 0m) - (creditNotes?.Taxable ?? 0m)),
            outputCgst,
            outputSgst,
            outputIgst,
            outputTotal,
            Round((input?.Taxable ?? 0m) - (debitNotes?.Taxable ?? 0m)),
            inputCgst,
            inputSgst,
            inputIgst,
            inputTotal,
            // Can go negative — a month of heavy stocking leaves credit carried forward rather
            // than tax to pay, and hiding that behind a zero would misstate the position.
            outputTotal - inputTotal,
            hsn);
    }

    public async Task<AuditChecksDto> GetAuditChecksAsync(
        DateOnly asOf,
        DateOnly monthStart,
        DateOnly monthEnd,
        string financialYear,
        decimal highValueThreshold,
        CancellationToken cancellationToken)
    {
        // A hole in the numbering is the first thing an auditor looks for. Cancelled documents keep
        // their number, so a gap means a row that was never written rather than one that was voided.
        var invoiceSequences = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.FinancialYear == financialYear)
            .Select(i => i.Sequence)
            .ToListAsync(cancellationToken);

        var purchaseSequences = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.FinancialYear == financialYear)
            .Select(p => p.Sequence)
            .ToListAsync(cancellationToken);

        var creditNoteSequences = await _context.CreditNotes
            .AsNoTracking()
            .Where(n => n.FinancialYear == financialYear)
            .Select(n => n.Sequence)
            .ToListAsync(cancellationToken);

        var debitNoteSequences = await _context.DebitNotes
            .AsNoTracking()
            .Where(n => n.FinancialYear == financialYear)
            .Select(n => n.Sequence)
            .ToListAsync(cancellationToken);

        var missingInvoices = DocumentNumbering.FindGaps(invoiceSequences);
        var missingPurchases = DocumentNumbering.FindGaps(purchaseSequences);
        var missingCreditNotes = DocumentNumbering.FindGaps(creditNoteSequences);
        var missingDebitNotes = DocumentNumbering.FindGaps(debitNoteSequences);

        var monthInvoices = _context.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate <= monthEnd);

        var invoiceChecks = await monthInvoices
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cancelled = g.Count(i => i.Status == InvoiceStatus.Cancelled),
                B2BCount = g.Count(i => i.Status != InvoiceStatus.Cancelled && i.CustomerGstin != null),
                B2BSales = g.Sum(i =>
                    i.Status != InvoiceStatus.Cancelled && i.CustomerGstin != null ? i.GrandTotal : 0m),
                B2CCount = g.Count(i => i.Status != InvoiceStatus.Cancelled && i.CustomerGstin == null),
                B2CSales = g.Sum(i =>
                    i.Status != InvoiceStatus.Cancelled && i.CustomerGstin == null ? i.GrandTotal : 0m),
                HighValueNoGstin = g.Count(i =>
                    i.Status != InvoiceStatus.Cancelled
                    && i.CustomerGstin == null
                    && i.GrandTotal >= highValueThreshold),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var cancelledPurchases = await _context.Purchases
            .AsNoTracking()
            .CountAsync(
                p => p.Status == PurchaseStatus.Cancelled
                     && p.InvoiceDate >= monthStart && p.InvoiceDate <= monthEnd,
                cancellationToken);

        // Stock that moved without a document behind it. Every one of these is a human decision and
        // is exactly what an auditor asks to see explained.
        var monthStartUtc = new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var monthEndUtc = new DateTimeOffset(
            monthEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // A line billed with no HSN cannot be reported in GSTR-1 Table 12. Counted distinctly by
        // product, because the question is how many items need fixing in the master, not how many
        // times they happened to sell.
        var missingHsn = await SalesLines(monthStart, monthEnd)
            .Where(l => l.Hsn == string.Empty)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Products = g.Select(l => l.ProductId).Distinct().Count(),
                Value = g.Sum(l => l.TaxableAmount),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var adjustments = await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.MovementType == StockMovementType.Adjustment
                        && m.MovedAt >= monthStartUtc && m.MovedAt < monthEndUtc)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), NetQuantity = g.Sum(m => m.Quantity) })
            .FirstOrDefaultAsync(cancellationToken);

        var reconciliation = await CheckReconciliationAsync(cancellationToken);

        return new AuditChecksDto(
            financialYear,
            missingInvoices.Take(10).Select(s => $"INV/{financialYear}/{s:D4}").ToList(),
            missingInvoices.Count,
            missingPurchases.Take(10).Select(s => $"PUR/{financialYear}/{s:D4}").ToList(),
            missingPurchases.Count,
            missingCreditNotes.Take(10).Select(s => $"CRN/{financialYear}/{s:D4}").ToList(),
            missingCreditNotes.Count,
            missingDebitNotes.Take(10).Select(s => $"DBN/{financialYear}/{s:D4}").ToList(),
            missingDebitNotes.Count,
            invoiceChecks?.Cancelled ?? 0,
            cancelledPurchases,
            adjustments?.Count ?? 0,
            adjustments?.NetQuantity ?? 0m,
            invoiceChecks?.B2BCount ?? 0,
            Round(invoiceChecks?.B2BSales ?? 0m),
            invoiceChecks?.B2CCount ?? 0,
            Round(invoiceChecks?.B2CSales ?? 0m),
            invoiceChecks?.HighValueNoGstin ?? 0,
            highValueThreshold,
            missingHsn?.Products ?? 0,
            Round(missingHsn?.Value ?? 0m),
            reconciliation);
    }

    /// <summary>
    /// Counts every row whose cached total no longer matches the entries it was derived from.
    /// <para>
    /// Four denormalised figures carry this shop's numbers — a party's balance, a document's balance,
    /// what a document has been paid, and what is on the shelf. Each is fast precisely because it is
    /// not recomputed, and each is therefore one missed write away from being quietly wrong. Nothing
    /// else in the app would notice; the first sign would be a customer disputing his balance.
    /// </para>
    /// </summary>
    private async Task<ReconciliationChecksDto> CheckReconciliationAsync(
        CancellationToken cancellationToken)
    {
        var customerDrift = await _context.Customers
            .AsNoTracking()
            .CountAsync(
                c => c.OutstandingBalance != _context.PartyLedgerEntries
                    .Where(e => e.CustomerId == c.Id)
                    .Sum(e => (decimal?)e.Amount).GetValueOrDefault(),
                cancellationToken);

        var supplierDrift = await _context.Suppliers
            .AsNoTracking()
            .CountAsync(
                s => s.OutstandingBalance != _context.PartyLedgerEntries
                    .Where(e => e.SupplierId == s.Id)
                    .Sum(e => (decimal?)e.Amount).GetValueOrDefault(),
                cancellationToken);

        // Three terms: billed, less paid, less returned. Cancelled documents are held to the other
        // half of the rule — they owe nothing regardless of what they once collected or credited.
        var invoiceBalanceDrift = await _context.Invoices
            .AsNoTracking()
            .CountAsync(
                i => i.Status == InvoiceStatus.Cancelled
                    ? i.BalanceDue != 0
                    : i.BalanceDue != i.GrandTotal - i.AmountPaid - i.CreditAppliedAmount,
                cancellationToken);

        var purchaseBalanceDrift = await _context.Purchases
            .AsNoTracking()
            .CountAsync(
                p => p.Status == PurchaseStatus.Cancelled
                    ? p.BalanceDue != 0
                    : p.BalanceDue != p.GrandTotal - p.AmountPaid - p.DebitAppliedAmount,
                cancellationToken);

        // What the bill says has come back, against the notes that say it. A cancelled note keeps
        // its figures as a record of what it did, so it is excluded rather than counted as zero.
        var creditAppliedDrift = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .CountAsync(
                i => i.CreditAppliedAmount != _context.CreditNotes
                    .Where(n => n.InvoiceId == i.Id && n.Status != CreditNoteStatus.Cancelled)
                    .Sum(n => (decimal?)n.AppliedToInvoiceAmount).GetValueOrDefault(),
                cancellationToken);

        var debitAppliedDrift = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.Status != PurchaseStatus.Cancelled)
            .CountAsync(
                p => p.DebitAppliedAmount != _context.DebitNotes
                    .Where(n => n.PurchaseId == p.Id && n.Status != DebitNoteStatus.Cancelled)
                    .Sum(n => (decimal?)n.AppliedToPurchaseAmount).GetValueOrDefault(),
                cancellationToken);

        // The quantity half of the same fact. This is what the over-return guard reads, so a drift
        // here would let goods come back twice.
        var returnedQuantityDrift = await _context.InvoiceItems
            .AsNoTracking()
            .CountAsync(
                item => item.ReturnedQuantity != _context.CreditNoteItems
                    .Where(ci => ci.InvoiceItemId == item.Id
                                 && _context.CreditNotes.Any(n => n.Id == ci.CreditNoteId
                                                                  && n.Status != CreditNoteStatus.Cancelled))
                    .Sum(ci => (decimal?)ci.Quantity).GetValueOrDefault(),
                cancellationToken);

        var returnedPurchaseQuantityDrift = await _context.PurchaseItems
            .AsNoTracking()
            .CountAsync(
                item => item.ReturnedQuantity != _context.DebitNoteItems
                    .Where(di => di.PurchaseItemId == item.Id
                                 && _context.DebitNotes.Any(n => n.Id == di.DebitNoteId
                                                                 && n.Status != DebitNoteStatus.Cancelled))
                    .Sum(di => (decimal?)di.Quantity).GetValueOrDefault(),
                cancellationToken);

        var invoiceAllocationDrift = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .CountAsync(
                i => i.AmountPaid != _context.PaymentAllocations
                    .Where(a => a.InvoiceId == i.Id && !a.IsReversed)
                    .Sum(a => (decimal?)a.Amount).GetValueOrDefault(),
                cancellationToken);

        var purchaseAllocationDrift = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.Status != PurchaseStatus.Cancelled)
            .CountAsync(
                p => p.AmountPaid != _context.PaymentAllocations
                    .Where(a => a.PurchaseId == p.Id && !a.IsReversed)
                    .Sum(a => (decimal?)a.Amount).GetValueOrDefault(),
                cancellationToken);

        // The same class of bug on the goods side, checked here because it has exactly the same
        // shape and exactly the same silence when it goes wrong.
        var stockDrift = await _context.Products
            .AsNoTracking()
            .CountAsync(
                p => p.StockOnHand != _context.StockMovements
                    .Where(m => m.ProductId == p.Id)
                    .Sum(m => (decimal?)m.Quantity).GetValueOrDefault(),
                cancellationToken);

        var creditRefundDrift = await _context.CreditNotes
            .AsNoTracking()
            .Where(n => n.Status != CreditNoteStatus.Cancelled)
            .CountAsync(
                n => n.RefundedAmount != _context.PaymentAllocations
                    .Where(a => a.CreditNoteId == n.Id && !a.IsReversed)
                    .Sum(a => (decimal?)a.Amount).GetValueOrDefault(),
                cancellationToken);

        var debitRefundDrift = await _context.DebitNotes
            .AsNoTracking()
            .Where(n => n.Status != DebitNoteStatus.Cancelled)
            .CountAsync(
                n => n.RefundedAmount != _context.PaymentAllocations
                    .Where(a => a.DebitNoteId == n.Id && !a.IsReversed)
                    .Sum(a => (decimal?)a.Amount).GetValueOrDefault(),
                cancellationToken);

        // Folded into the existing four buckets rather than adding a fifth: the panel's job is one
        // green light, and a reader who sees "document balances" does not care which of the several
        // figures behind that phrase drifted — only that something did.
        return new ReconciliationChecksDto(
            customerDrift + supplierDrift,
            invoiceBalanceDrift + purchaseBalanceDrift
                + creditAppliedDrift + debitAppliedDrift
                + returnedQuantityDrift + returnedPurchaseQuantityDrift,
            invoiceAllocationDrift + purchaseAllocationDrift + creditRefundDrift + debitRefundDrift,
            stockDrift);
    }

    public async Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled
                        && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
            .GroupBy(i => i.InvoiceDate)
            .Select(g => new { Date = g.Key, Total = g.Sum(i => i.GrandTotal), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new SalesTrendPointDto(r.Date, Round(r.Total), r.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<TopSellingItemDto>> GetTopSellingAsync(
        DateOnly fromDate, DateOnly toDate, int limit, CancellationToken cancellationToken)
    {
        var rows = await SalesLines(fromDate, toDate)
            .GroupBy(x => new { x.ProductId, x.PartNumber, x.ItemName, x.Uqc })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.PartNumber,
                g.Key.ItemName,
                g.Key.Uqc,
                Quantity = g.Sum(i => i.Quantity),
                SalesValue = g.Sum(i => i.TaxableAmount),
            })
            // Ranked by value, not by count: twenty cables moving is worth less attention than one
            // expensive part, and reordering follows the money.
            .OrderByDescending(r => r.SalesValue)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new TopSellingItemDto(
                r.ProductId, r.PartNumber, r.ItemName, r.Uqc, r.Quantity, Round(r.SalesValue)))
            .ToList();
    }

    public async Task<IReadOnlyList<RecentInvoiceDto>> GetRecentInvoicesAsync(
        int limit, CancellationToken cancellationToken)
    {
        var rows = await _context.Invoices
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Sequence)
            .Take(limit)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.CustomerName,
                i.GrandTotal,
                i.BalanceDue,
                i.Status,
            })
            .ToListAsync(cancellationToken);

        // Status is stringified here rather than in the query — the column is stored as text, but
        // ToString() on the enum is not something EF will translate.
        return rows
            .Select(r => new RecentInvoiceDto(
                r.Id,
                r.InvoiceNumber,
                r.InvoiceDate,
                r.CustomerName,
                r.GrandTotal,
                r.BalanceDue,
                r.Status.ToString()))
            .ToList();
    }

    /// <summary>
    /// Invoice lines of non-cancelled sales in a date range, flattened off the join so the caller can
    /// group them. Shared by the HSN summary and the top-sellers list, which differ only in the key.
    /// </summary>
    private IQueryable<SalesLineRow> SalesLines(DateOnly fromDate, DateOnly toDate) =>
        from item in _context.InvoiceItems.AsNoTracking()
        join invoice in _context.Invoices.AsNoTracking() on item.InvoiceId equals invoice.Id
        where invoice.Status != InvoiceStatus.Cancelled
              && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate <= toDate
        select new SalesLineRow
        {
            ProductId = item.ProductId,
            PartNumber = item.PartNumber,
            ItemName = item.ItemName,
            Hsn = item.Hsn,
            Uqc = item.Uqc,
            Quantity = item.Quantity,
            TaxableAmount = item.TaxableAmount,
            CgstAmount = item.CgstAmount,
            SgstAmount = item.SgstAmount,
            IgstAmount = item.IgstAmount,
        };

    public async Task<(decimal Revenue, decimal CostOfGoods, int CostedLines, int UncostedLines)>
        GetTradingResultAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        // Revenue is the taxable value, not the grand total: GST collected is the government's, not
        // the shop's, and counting it as income overstates every margin below.
        var sold = await (
            from item in _context.InvoiceItems.AsNoTracking()
            join invoice in _context.Invoices.AsNoTracking() on item.InvoiceId equals invoice.Id
            where invoice.Status != InvoiceStatus.Cancelled
                  && invoice.InvoiceDate >= fromDate && invoice.InvoiceDate <= toDate
            select new { item.TaxableAmount, item.Quantity, item.CostRate })
            .ToListAsync(cancellationToken);

        // Goods that came back are neither revenue nor cost.
        var returned = await (
            from line in _context.CreditNoteItems.AsNoTracking()
            join note in _context.CreditNotes.AsNoTracking() on line.CreditNoteId equals note.Id
            where note.Status != CreditNoteStatus.Cancelled
                  && note.NoteDate >= fromDate && note.NoteDate <= toDate
            select new { line.TaxableAmount, line.Quantity, line.CostRate })
            .ToListAsync(cancellationToken);

        var revenue = sold.Sum(l => l.TaxableAmount) - returned.Sum(l => l.TaxableAmount);

        // Only lines that actually carry a cost contribute. A line sold before cost was captured
        // has none, and inventing one — from today's purchase rate, or from zero — would quietly
        // turn a guess into a reported profit.
        var cost = sold.Where(l => l.CostRate.HasValue).Sum(l => l.CostRate!.Value * l.Quantity)
                   - returned.Where(l => l.CostRate.HasValue).Sum(l => l.CostRate!.Value * l.Quantity);

        return (
            Round(revenue),
            Round(cost),
            sold.Count(l => l.CostRate.HasValue),
            sold.Count(l => !l.CostRate.HasValue));
    }

    /// <summary>
    /// Credit-note lines in the same shape as <see cref="SalesLines"/>, so the HSN summary can be
    /// netted without a second row type.
    /// </summary>
    private IQueryable<SalesLineRow> ReturnLines(DateOnly fromDate, DateOnly toDate) =>
        from item in _context.CreditNoteItems.AsNoTracking()
        join note in _context.CreditNotes.AsNoTracking() on item.CreditNoteId equals note.Id
        where note.Status != CreditNoteStatus.Cancelled
              && note.NoteDate >= fromDate && note.NoteDate <= toDate
        select new SalesLineRow
        {
            ProductId = item.ProductId,
            PartNumber = item.PartNumber,
            ItemName = item.ItemName,
            Hsn = item.Hsn,
            Uqc = item.Uqc,
            Quantity = item.Quantity,
            TaxableAmount = item.TaxableAmount,
            CgstAmount = item.CgstAmount,
            SgstAmount = item.SgstAmount,
            IgstAmount = item.IgstAmount,
        };

    private sealed class SalesLineRow
    {
        public Guid ProductId { get; init; }
        public string PartNumber { get; init; } = string.Empty;
        public string ItemName { get; init; } = string.Empty;
        public string Hsn { get; init; } = string.Empty;
        public string Uqc { get; init; } = string.Empty;
        public decimal Quantity { get; init; }
        public decimal TaxableAmount { get; init; }
        public decimal CgstAmount { get; init; }
        public decimal SgstAmount { get; init; }
        public decimal IgstAmount { get; init; }
    }

    /// <summary>
    /// Sales and purchases over a date range, in two grouped queries. Shared by the day tile and the
    /// month tile so the two can never be computed differently.
    /// </summary>
    private async Task<(decimal Sales, int InvoiceCount, decimal Purchases, int PurchaseCount)>
        GetTotalsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var sales = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled
                        && i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(i => i.GrandTotal), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var purchases = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.Status != PurchaseStatus.Cancelled
                        && p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(p => p.GrandTotal), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return (
            Round(sales?.Total ?? 0m),
            sales?.Count ?? 0,
            Round(purchases?.Total ?? 0m),
            purchases?.Count ?? 0);
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
