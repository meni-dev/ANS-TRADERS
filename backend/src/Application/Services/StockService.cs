using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Stock;
using Application.Interfaces;
using Application.Mapping;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedger _ledger;
    private readonly IPeriodLock _periodLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<AdjustStockRequest> _adjustValidator;

    private readonly IShopClock _clock;

    public StockService(
        IStockRepository repository,
        IProductRepository productRepository,
        IStockLedger ledger,
        IPeriodLock periodLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IValidator<AdjustStockRequest> adjustValidator,
        IShopClock clock)
    {
        _repository = repository;
        _productRepository = productRepository;
        _ledger = ledger;
        _periodLock = periodLock;
        _audit = audit;
        _currentUser = currentUser;
        _adjustValidator = adjustValidator;
        _clock = clock;
    }

    public async Task<PagedResult<ProductStockDto>> SearchAsync(
        StockListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchStockAsync(
            query.Search, query.LowOnly, query.ActiveOnly, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<ProductStockDto>(
            items.Select(p => p.ToStockDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<StockSummaryDto> GetSummaryAsync(StockListQuery query, CancellationToken cancellationToken)
    {
        var (totalItems, lowStockCount, outOfStockCount, totalStockValue) =
            await _repository.GetStockSummaryAsync(
                query.Search, query.LowOnly, query.ActiveOnly, cancellationToken);

        return new StockSummaryDto(totalItems, lowStockCount, outOfStockCount, totalStockValue);
    }

    public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(
        StockMovementListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchMovementsAsync(
            query.Search,
            query.ProductId,
            ParseMovementType(query.MovementType),
            query.FromDate,
            query.ToDate,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new PagedResult<StockMovementDto>(
            items.Select(m => m.ToDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<ProductStockDto> AdjustAsync(
        AdjustStockRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_adjustValidator, request, cancellationToken);

        // An adjustment is always dated now, so this only bites when the lock reaches today —
        // which is exactly the month-end freeze it exists for.
        _currentUser.Require(Permission.StockAdjust, "correct the stock on the shelf");

        await _periodLock.GuardAsync(_clock.Today, "stock adjustment", cancellationToken);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.ProductId}' was not found", "PRODUCT_NOT_FOUND");

        var difference = request.CountedQuantity - product.StockOnHand;

        // A recount that matches is not an error, but writing a zero-quantity row would clutter the
        // ledger with movements that moved nothing.
        if (difference == 0)
        {
            throw new ConflictException(
                $"'{product.ItemName}' is already recorded at {request.CountedQuantity:0.###}",
                "STOCK_ALREADY_MATCHES");
        }

        if (!Enum.TryParse<StockAdjustmentReason>(request.Reason, ignoreCase: true, out var reason))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Reason"] = ["Pick why the count is being corrected"],
            });
        }

        await _ledger.RecordAsync(
            product,
            difference,
            StockMovementType.Adjustment,
            referenceId: null,
            referenceNumber: null,
            notes: string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            cancellationToken,
            adjustmentReason: reason);

        await _audit.RecordAsync(
            AuditAction.StockAdjusted,
            "Product",
            product.Id,
            product.ItemName,
            $"{product.StockOnHand - difference:0.###} to {request.CountedQuantity:0.###} ({reason})",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return product.ToStockDto();
    }

    private static StockMovementType? ParseMovementType(string? movementType) =>
        Enum.TryParse<StockMovementType>(movementType, ignoreCase: true, out var parsed) ? parsed : null;

    public async Task<StockLossReportDto> GetLossReportAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var adjustments = await _repository.GetAdjustmentsAsync(fromDate, toDate, cancellationToken);

        // Only the ones that took stock away. A counting error that *found* stock is not a loss, and
        // netting the two would let a good month's recount hide a bad month's breakage.
        var losses = adjustments
            .Where(a => a.Quantity < 0)
            .Select(a => new StockLossRowDto(
                a.Reason.ToString(),
                LossLabel(a.Reason),
                Math.Abs(a.Quantity),
                Math.Round(Math.Abs(a.Value), 2, MidpointRounding.AwayFromZero),
                1))
            .GroupBy(r => r.Reason)
            .Select(g => new StockLossRowDto(
                g.Key,
                g.First().Label,
                g.Sum(r => r.Quantity),
                g.Sum(r => r.Value),
                g.Count()))
            .OrderByDescending(r => r.Value)
            .ToList();

        return new StockLossReportDto(
            fromDate, toDate, losses.Sum(r => r.Value), losses);
    }

    /// <summary>What the reason is called on screen.</summary>
    public static string LossLabel(StockAdjustmentReason reason) => reason switch
    {
        StockAdjustmentReason.CountingError => "Counting error",
        StockAdjustmentReason.Damage => "Damaged",
        StockAdjustmentReason.Expiry => "Expired",
        StockAdjustmentReason.TheftOrMissing => "Missing or taken",
        StockAdjustmentReason.FreeIssue => "Given free",
        StockAdjustmentReason.Scrapped => "Scrapped",
        _ => "Other",
    };
}
