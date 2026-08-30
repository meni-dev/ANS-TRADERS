using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CreditNoteRepository : ICreditNoteRepository
{
    private readonly AppDbContext _context;

    public CreditNoteRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <remarks>Tracked and with its lines: cancelling the note has to move every one of them.</remarks>
    public Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.CreditNotes
            .Include(n => n.Items)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<CreditNote> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? customerId,
        Guid? invoiceId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.CreditNotes.AsNoTracking().AsQueryable();

        if (customerId is { } c) query = query.Where(n => n.CustomerId == c);
        if (invoiceId is { } i) query = query.Where(n => n.InvoiceId == i);
        if (fromDate is { } from) query = query.Where(n => n.NoteDate >= from);
        if (toDate is { } to) query = query.Where(n => n.NoteDate <= to);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.CreditNoteNumber, pattern) ||
                EF.Functions.ILike(n.InvoiceNumber, pattern) ||
                EF.Functions.ILike(n.CustomerName, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first, like every other document list in the app.
            .OrderByDescending(n => n.NoteDate)
            .ThenByDescending(n => n.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <remarks>
    /// Cancelled notes are included on purpose: they keep their number, so a gap always means a row
    /// that was never written rather than one that was voided.
    /// </remarks>
    public async Task<IReadOnlyList<int>> GetSequencesAsync(
        string financialYear, CancellationToken cancellationToken) =>
        await _context.CreditNotes
            .AsNoTracking()
            .Where(n => n.FinancialYear == financialYear)
            .Select(n => n.Sequence)
            .ToListAsync(cancellationToken);

    public Task<bool> HasLiveNotesForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        _context.CreditNotes.AnyAsync(
            n => n.InvoiceId == invoiceId && n.Status != CreditNoteStatus.Cancelled, cancellationToken);

    public async Task AddAsync(CreditNote note, CancellationToken cancellationToken) =>
        await _context.CreditNotes.AddAsync(note, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
