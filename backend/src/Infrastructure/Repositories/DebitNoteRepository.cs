using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DebitNoteRepository : IDebitNoteRepository
{
    private readonly AppDbContext _context;

    public DebitNoteRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <remarks>Tracked and with its lines: cancelling the note has to move every one of them.</remarks>
    public Task<DebitNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.DebitNotes
            .Include(n => n.Items)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<DebitNote> Items, int TotalCount)> SearchAsync(
        string? search,
        Guid? supplierId,
        Guid? purchaseId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.DebitNotes.AsNoTracking().AsQueryable();

        if (supplierId is { } c) query = query.Where(n => n.SupplierId == c);
        if (purchaseId is { } i) query = query.Where(n => n.PurchaseId == i);
        if (fromDate is { } from) query = query.Where(n => n.NoteDate >= from);
        if (toDate is { } to) query = query.Where(n => n.NoteDate <= to);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.DebitNoteNumber, pattern) ||
                EF.Functions.ILike(n.PurchaseNumber, pattern) ||
                EF.Functions.ILike(n.SupplierName, pattern));
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
        await _context.DebitNotes
            .AsNoTracking()
            .Where(n => n.FinancialYear == financialYear)
            .Select(n => n.Sequence)
            .ToListAsync(cancellationToken);

    public Task<bool> HasLiveNotesForPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken) =>
        _context.DebitNotes.AnyAsync(
            n => n.PurchaseId == purchaseId && n.Status != DebitNoteStatus.Cancelled, cancellationToken);

    public async Task AddAsync(DebitNote note, CancellationToken cancellationToken) =>
        await _context.DebitNotes.AddAsync(note, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
