using Application.Interfaces;
using Domain.Entities;
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

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        // Movements are stamped with an instant, not a date, so the range is converted once here.
        // The end is exclusive of the following midnight rather than inclusive of 23:59:59, which
        // would silently drop anything in the last second of the day.
        var fromUtc = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        return await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.MovedAt >= fromUtc && m.MovedAt < toUtc)
            .OrderBy(m => m.MovedAt)
            .ToListAsync(cancellationToken);
    }
}
