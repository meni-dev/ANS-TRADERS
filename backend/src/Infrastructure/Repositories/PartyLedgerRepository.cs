using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PartyLedgerRepository : IPartyLedgerRepository
{
    private readonly AppDbContext _context;

    public PartyLedgerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddEntryAsync(PartyLedgerEntry entry, CancellationToken cancellationToken) =>
        await _context.PartyLedgerEntries.AddAsync(entry, cancellationToken);

    public async Task<(IReadOnlyList<PartyLedgerEntry> Items, int TotalCount, decimal OpeningBalance,
            decimal RangeMovement, decimal CarriedIn)>
        GetStatementAsync(
            Guid? customerId,
            Guid? supplierId,
            DateOnly? fromDate,
            DateOnly? toDate,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var forParty = ForParty(customerId, supplierId);

        // What the account stood at going into the range. A statement that starts at zero when the
        // account did not is worse than no statement — the customer will simply not recognise it.
        var openingBalance = fromDate is { } from
            ? await forParty
                .Where(e => e.EntryDate < from)
                .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m
            : 0m;

        var query = forParty;

        if (fromDate is { } start) query = query.Where(e => e.EntryDate >= start);
        if (toDate is { } end) query = query.Where(e => e.EntryDate <= end);

        var totalCount = await query.CountAsync(cancellationToken);

        // Over the range, not the page — see the note on the interface.
        var rangeMovement = await query.SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        // Oldest first: a statement is read top to bottom, unlike every list screen in the app.
        // Ordered by system time so same-day rows keep the order they actually happened in.
        var ordered = query.OrderBy(e => e.EntryDate).ThenBy(e => e.RecordedAt);

        var skip = (page - 1) * pageSize;

        // Everything in the range that sits above this page, so page 3's running column continues
        // from page 2 instead of restarting at the range's opening figure.
        var beforePage = skip == 0
            ? 0m
            : await ordered.Take(skip).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var items = await ordered
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, Round(openingBalance), Round(rangeMovement),
            Round(openingBalance + beforePage));
    }

    public async Task<decimal> SumForPartyAsync(
        Guid? customerId, Guid? supplierId, CancellationToken cancellationToken) =>
        Round(await ForParty(customerId, supplierId)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    private IQueryable<PartyLedgerEntry> ForParty(Guid? customerId, Guid? supplierId) =>
        _context.PartyLedgerEntries
            .AsNoTracking()
            .Where(e => customerId != null
                ? e.CustomerId == customerId
                : e.SupplierId == supplierId);

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
