using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync(
        DateOnly fromDate, DateOnly toDate, bool withItems, CancellationToken cancellationToken)
    {
        // Cancelled documents stay in the register rather than disappearing from it. A number that
        // exists but shows no figures is what tells the accountant the series has no hole in it;
        // a missing row looks like a document that was deleted.
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate);

        if (withItems)
        {
            query = query.Include(i => i.Items);
        }

        return await query
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Purchase>> GetPurchasesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.Purchases
            .AsNoTracking()
            .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
            .OrderBy(p => p.InvoiceDate)
            .ThenBy(p => p.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CreditNote>> GetCreditNotesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.CreditNotes
            .AsNoTracking()
            // With their lines: Table 8 and 3B decide line by line which are taxable and which are
            // not, and a note loaded without them silently nets nothing at all.
            .Include(n => n.Items)
            .Where(n => n.NoteDate >= fromDate && n.NoteDate <= toDate)
            .OrderBy(n => n.NoteDate)
            .ThenBy(n => n.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DebitNote>> GetDebitNotesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.DebitNotes
            .AsNoTracking()
            .Where(n => n.NoteDate >= fromDate && n.NoteDate <= toDate)
            .OrderBy(n => n.NoteDate)
            .ThenBy(n => n.Sequence)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.Payments
            .AsNoTracking()
            // Money taken over the counter carries no receipt number — the bill is the document —
            // so without the allocations most of this register would be rows with nothing on them
            // to say what they were for.
            .Include(p => p.Allocations)
            .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Expense>> GetExpensesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate)
            .OrderBy(e => e.ExpenseDate)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Customer> Customers, IReadOnlyList<Supplier> Suppliers)>
        GetOpenPartiesAsync(CancellationToken cancellationToken)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => c.OutstandingBalance != 0)
            .OrderByDescending(c => c.OutstandingBalance)
            .ToListAsync(cancellationToken);

        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.OutstandingBalance != 0)
            .OrderByDescending(s => s.OutstandingBalance)
            .ToListAsync(cancellationToken);

        return (customers, suppliers);
    }

    public async Task<IReadOnlyList<Product>> GetProductsForValuationAsync(
        CancellationToken cancellationToken) =>
        await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive || p.StockOnHand != 0)
            .OrderBy(p => p.PartNumber)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken) =>
        await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.BalanceDue > 0)
            .OrderBy(i => i.CustomerName)
            .ThenBy(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetStockBalancesOnAsync(
        DateOnly onDate, CancellationToken cancellationToken) =>
        await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.MovementDate <= onDate)
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Balance = g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Balance, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPartyBalancesOnAsync(
        DateOnly onDate, bool customers, CancellationToken cancellationToken) =>
        await _context.PartyLedgerEntries
            .AsNoTracking()
            .Where(e => e.EntryDate <= onDate
                        && (customers ? e.CustomerId != null : e.SupplierId != null))
            .GroupBy(e => customers ? e.CustomerId!.Value : e.SupplierId!.Value)
            .Select(g => new { PartyId = g.Key, Balance = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.PartyId, x => x.Balance, cancellationToken);

    public async Task<decimal> GetStockBalanceBeforeAsync(
        Guid productId, DateOnly fromDate, CancellationToken cancellationToken) =>
        await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.ProductId == productId && m.MovementDate < fromDate)
            .SumAsync(m => (decimal?)m.Quantity, cancellationToken) ?? 0m;

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        // On the document's date, not on when the row was written — so this register and the sales
        // register agree about the week the shelf emptied. MovementDate is already a shop day, so
        // there is no timezone arithmetic left to get wrong.
        return await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.MovementDate >= fromDate && m.MovementDate <= toDate)
            .OrderBy(m => m.MovementDate)
            .ThenBy(m => m.MovedAt)
            .ToListAsync(cancellationToken);
    }
}
