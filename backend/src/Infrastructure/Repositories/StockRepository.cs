using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken) =>
        await _context.StockMovements.AddAsync(movement, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchStockAsync(
        string? search, bool? lowOnly, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = BuildStockQuery(search, lowOnly, activeOnly);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Emptiest shelf first: the whole point of the screen is what needs reordering, and
            // that is buried if the list is alphabetical.
            .OrderBy(p => p.StockOnHand)
            .ThenBy(p => p.ItemName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(int TotalItems, int LowStockCount, int OutOfStockCount, decimal TotalStockValue)>
        GetStockSummaryAsync(string? search, bool? lowOnly, bool? activeOnly, CancellationToken cancellationToken)
    {
        var query = BuildStockQuery(search, lowOnly, activeOnly);

        // One round trip rather than four counts: the stock screen loads these alongside a page of
        // rows, and each extra query is another wait before the user sees anything.
        var summary = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalItems = g.Count(),
                LowStockCount = g.Count(p => p.StockOnHand > 0 && p.StockOnHand <= p.ReorderLevel),
                OutOfStockCount = g.Count(p => p.StockOnHand <= 0),
                TotalStockValue = g.Sum(p => p.StockOnHand * p.PurchaseRate),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary is null
            ? (0, 0, 0, 0m)
            : (summary.TotalItems, summary.LowStockCount, summary.OutOfStockCount,
               Math.Round(summary.TotalStockValue, 2, MidpointRounding.AwayFromZero));
    }

    public async Task<(IReadOnlyList<StockMovement> Items, int TotalCount)> SearchMovementsAsync(
        string? search,
        Guid? productId,
        StockMovementType? movementType,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.StockMovements.AsNoTracking();

        if (productId is { } product)
        {
            query = query.Where(m => m.ProductId == product);
        }

        if (movementType is { } type)
        {
            query = query.Where(m => m.MovementType == type);
        }

        if (fromDate is { } from)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(m => m.MovedAt >= fromUtc);
        }

        if (toDate is { } to)
        {
            // Exclusive upper bound on the next day, so a movement at 23:59 on the end date is not
            // silently dropped the way `<= midnight` would drop it.
            var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(m => m.MovedAt < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.PartNumber, pattern) ||
                EF.Functions.ILike(m.ItemName, pattern) ||
                (m.ReferenceNumber != null && EF.Functions.ILike(m.ReferenceNumber, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.MovedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<ProductShelfFacts>> GetShelfFactsAsync(
        DateOnly asOf, int velocityWindowDays, CancellationToken cancellationToken)
    {
        var windowStart = asOf.AddDays(-velocityWindowDays);

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive || p.StockOnHand != 0)
            .OrderBy(p => p.PartNumber)
            .ToListAsync(cancellationToken);

        // Grouped queries rather than a correlated subquery per product: a shop with thousands of
        // parts would otherwise issue thousands of round trips to draw one screen.
        var lastSold = await (
                from item in _context.InvoiceItems
                join invoice in _context.Invoices on item.InvoiceId equals invoice.Id
                where invoice.Status != InvoiceStatus.Cancelled
                group invoice.InvoiceDate by item.ProductId into g
                select new { ProductId = g.Key, LastSoldOn = g.Max() })
            .ToDictionaryAsync(x => x.ProductId, x => x.LastSoldOn, cancellationToken);

        var sold = await (
                from item in _context.InvoiceItems
                join invoice in _context.Invoices on item.InvoiceId equals invoice.Id
                where invoice.Status != InvoiceStatus.Cancelled
                    && invoice.InvoiceDate > windowStart && invoice.InvoiceDate <= asOf
                group item by item.ProductId into g
                // Net of returns: a part sold and handed straight back has not moved, and counting
                // it as velocity would have the shop reordering something nobody kept.
                select new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity - x.ReturnedQuantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, cancellationToken);

        // Purchases are far fewer than sales in a parts shop — bought by the box, sold one at a
        // time — so pulling the rate rows and reducing them here is cheap, and it avoids a second
        // query to find the rate belonging to the newest date.
        var purchaseLines = await (
                from item in _context.PurchaseItems
                join purchase in _context.Purchases on item.PurchaseId equals purchase.Id
                where purchase.Status != PurchaseStatus.Cancelled
                select new { item.ProductId, purchase.InvoiceDate, item.Rate })
            .ToListAsync(cancellationToken);

        var lastPurchase = purchaseLines
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var newest = g.OrderByDescending(x => x.InvoiceDate).First();
                    return (newest.InvoiceDate, newest.Rate);
                });

        return products
            .Select(p => new ProductShelfFacts(
                p.Id,
                p.PartNumber,
                p.ItemName,
                p.VehicleBrand,
                p.StockOnHand,
                p.PurchaseRate,
                p.SellingRate,
                p.Mrp,
                p.ReorderLevel,
                p.IsActive,
                lastSold.TryGetValue(p.Id, out var soldOn) ? soldOn : null,
                sold.GetValueOrDefault(p.Id),
                lastPurchase.TryGetValue(p.Id, out var bought) ? bought.InvoiceDate : null,
                lastPurchase.TryGetValue(p.Id, out var rate) ? rate.Rate : null))
            .ToList();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Shared by the row query and the summary so the headline counts always describe exactly the
    /// set the user is looking at.
    /// </summary>
    private IQueryable<Product> BuildStockQuery(string? search, bool? lowOnly, bool? activeOnly)
    {
        var query = _context.Products.AsNoTracking();

        // Everything is included by default, deliberately: the screen reports what the shelf holds
        // and what it is worth, and hiding discontinued parts that are still physically there would
        // understate both.
        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        if (lowOnly == true)
        {
            // Reordering is the one question where a discontinued part is never the answer, so the
            // low-stock filter carries that rule rather than leaving it to the caller.
            query = query.Where(p => p.IsActive && p.StockOnHand <= p.ReorderLevel);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.PartNumber, pattern) ||
                EF.Functions.ILike(p.ItemName, pattern) ||
                (p.VehicleBrand != null && EF.Functions.ILike(p.VehicleBrand, pattern)) ||
                (p.VehicleModel != null && EF.Functions.ILike(p.VehicleModel, pattern)));
        }

        return query;
    }

    public async Task<IReadOnlyList<(StockAdjustmentReason Reason, decimal Quantity, decimal Value)>>
        GetAdjustmentsAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rows = await (
            from movement in _context.StockMovements.AsNoTracking()
            join product in _context.Products.AsNoTracking() on movement.ProductId equals product.Id
            where movement.MovementType == StockMovementType.Adjustment
                  && movement.AdjustmentReason != null
                  && movement.MovedAt >= fromUtc && movement.MovedAt < toUtc
            select new { movement.AdjustmentReason, movement.Quantity, product.PurchaseRate })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.AdjustmentReason!.Value)
            .Select(g => (
                Reason: g.Key,
                Quantity: g.Sum(r => r.Quantity),
                Value: g.Sum(r => r.Quantity * r.PurchaseRate)))
            .ToList();
    }
}
