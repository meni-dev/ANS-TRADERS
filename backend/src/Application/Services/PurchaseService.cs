using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Payments;
using Application.DTOs.Purchases;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class PurchaseService : IPurchaseService
{
    private readonly IPurchaseRepository _repository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedger _stockLedger;
    private readonly IShopSettingsRepository _shopSettings;
    private readonly IPaymentLedger _paymentLedger;
    private readonly IPartyLedger _partyLedger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IDebitNoteRepository _debitNotes;
    private readonly IPeriodLock _periodLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreatePurchaseRequest> _createValidator;

    public PurchaseService(
        IPurchaseRepository repository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IStockLedger stockLedger,
        IShopSettingsRepository shopSettings,
        IPaymentLedger paymentLedger,
        IPartyLedger partyLedger,
        IPaymentRepository paymentRepository,
        IDebitNoteRepository debitNotes,
        IPeriodLock periodLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IValidator<CreatePurchaseRequest> createValidator)
    {
        _repository = repository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _stockLedger = stockLedger;
        _shopSettings = shopSettings;
        _paymentLedger = paymentLedger;
        _partyLedger = partyLedger;
        _paymentRepository = paymentRepository;
        _debitNotes = debitNotes;
        _periodLock = periodLock;
        _audit = audit;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Reads are guarded here and almost nowhere else. A purchase bill prints what the shop pays its
    /// supplier, so opening one is the same act as looking up cost.
    /// </summary>
    public async Task<PagedResult<PurchaseListItemDto>> SearchAsync(
        PurchaseListQuery query, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PurchaseView, "see purchase bills");

        var (items, totalCount) = await _repository.SearchAsync(
            query.Search,
            ParseStatus(query.Status),
            query.FromDate,
            query.ToDate,
            query.SupplierId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new PagedResult<PurchaseListItemDto>(
            items.Select(p => p.ToListItemDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<PurchaseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PurchaseView, "see a purchase bill");

        var purchase = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Purchase '{id}' was not found", "PURCHASE_NOT_FOUND");

        return purchase.ToDto();
    }

    public async Task<PurchaseDto> CreateAsync(
        CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PurchaseCreate, "enter a purchase");

        await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);
        await _periodLock.GuardAsync(request.InvoiceDate, "purchase", cancellationToken);

        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new NotFoundException(
                $"Supplier '{request.SupplierId}' was not found", "SUPPLIER_NOT_FOUND");

        // The same bill entered twice is the most common data-entry mistake at a parts counter, and
        // it silently doubles the input tax credit claimed. Cheap to catch here.
        if (await _repository.SupplierInvoiceNumberExistsAsync(
                supplier.Id, request.SupplierInvoiceNumber.Trim(), cancellationToken))
        {
            throw new ConflictException(
                $"Bill '{request.SupplierInvoiceNumber}' is already recorded against {supplier.Name}",
                "DUPLICATE_SUPPLIER_INVOICE");
        }

        var shop = await _shopSettings.GetAsync(cancellationToken);
        var isInterState = GstCalculator.IsInterState(shop.StateCode, supplier.StateCode);

        var financialYear = FinancialYear.For(request.InvoiceDate);
        var sequence = await _repository.GetLastSequenceAsync(financialYear, cancellationToken) + 1;

        var purchase = new Purchase
        {
            PurchaseNumber = $"PUR/{financialYear}/{sequence:D4}",
            FinancialYear = financialYear,
            Sequence = sequence,
            SupplierInvoiceNumber = request.SupplierInvoiceNumber.Trim(),
            InvoiceDate = request.InvoiceDate,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            SupplierGstin = supplier.Gstin,
            SupplierStateCode = supplier.StateCode,
            IsInterState = isInterState,
            PaymentMode = ParsePaymentMode(request.PaymentMode),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = PurchaseStatus.Received,
        };

        var lineAmounts = new List<LineAmounts>(request.Items.Count);

        foreach (var line in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, cancellationToken)
                ?? throw new NotFoundException(
                    $"Product '{line.ProductId}' was not found", "PRODUCT_NOT_FOUND");

            // The GST rate comes from the product master, never from the request. A rate typed at
            // the counter is a filing error waiting to happen.
            var amounts = GstCalculator.ComputeLine(
                line.Quantity, line.Rate, line.DiscountPercent, product.GstRate, isInterState);

            lineAmounts.Add(amounts);

            purchase.Items.Add(new PurchaseItem
            {
                PurchaseId = purchase.Id,
                ProductId = product.Id,
                PartNumber = product.PartNumber,
                ItemName = product.ItemName,
                Hsn = product.Hsn,
                Uqc = product.Uqc,
                Quantity = line.Quantity,
                Rate = line.Rate,
                DiscountPercent = line.DiscountPercent,
                DiscountAmount = amounts.DiscountAmount,
                GrossAmount = amounts.GrossAmount,
                TaxableAmount = amounts.TaxableAmount,
                GstRate = product.GstRate,
                CgstAmount = amounts.CgstAmount,
                SgstAmount = amounts.SgstAmount,
                IgstAmount = amounts.IgstAmount,
                LineTotal = amounts.LineTotal,
            });

            // Goods are on the shelf the moment the bill is recorded. Nothing is persisted until
            // the save below, so a later line failing validation leaves stock untouched.
            await _stockLedger.RecordAsync(
                product,
                line.Quantity,
                StockMovementType.Purchase,
                purchase.Id,
                purchase.PurchaseNumber,
                notes: null,
                cancellationToken);
        }

        var totals = GstCalculator.ComputeDocument(lineAmounts);

        purchase.ItemCount = purchase.Items.Count;
        purchase.SubTotal = totals.SubTotal;
        purchase.DiscountAmount = totals.DiscountAmount;
        purchase.TaxableAmount = totals.TaxableAmount;
        purchase.CgstAmount = totals.CgstAmount;
        purchase.SgstAmount = totals.SgstAmount;
        purchase.IgstAmount = totals.IgstAmount;
        purchase.TotalTax = totals.TotalTax;
        purchase.RoundOff = totals.RoundOff;
        purchase.GrandTotal = totals.GrandTotal;

        // Paying more than the bill is always a typo, and letting it through turns the supplier
        // balance negative for no legitimate reason.
        if (request.AmountPaid > purchase.GrandTotal)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["AmountPaid"] = [$"Cannot exceed the bill total of {purchase.GrandTotal:0.00}"],
            });
        }

        // Born unpaid, exactly as an invoice is: whatever was settled at the door arrives below as a
        // payment, so the supplier's account has a row for every rupee.
        purchase.AmountPaid = 0;
        purchase.BalanceDue = purchase.GrandTotal;

        await _repository.AddAsync(purchase, cancellationToken);

        await _partyLedger.RecordForSupplierAsync(
            supplier,
            purchase.GrandTotal,
            PartyLedgerEntryType.PurchaseBill,
            purchase.InvoiceDate,
            purchase.Id,
            purchase.PurchaseNumber,
            notes: null,
            cancellationToken);

        if (request.AmountPaid > 0)
        {
            var tenderMode = purchase.PaymentMode == PaymentMode.Credit
                ? PaymentMode.Cash
                : purchase.PaymentMode;

            await _paymentLedger.RecordCounterPaymentAsync(
                new PaymentDraft(
                    PaymentDirection.Paid,
                    Customer: null,
                    supplier,
                    supplier.Name,
                    purchase.InvoiceDate,
                    request.AmountPaid,
                    tenderMode,
                    ReferenceNumber: null,
                    Notes: null,
                    IsCounterPayment: true,
                    Cheque: null,
                    [new AllocationTarget(null, purchase, request.AmountPaid)]),
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return purchase.ToDto();
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var purchase = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Purchase '{id}' was not found", "PURCHASE_NOT_FOUND");

        if (purchase.Status == PurchaseStatus.Cancelled)
        {
            throw new ConflictException(
                $"Purchase '{purchase.PurchaseNumber}' is already cancelled", "PURCHASE_ALREADY_CANCELLED");
        }

        _currentUser.Require(Permission.PurchaseCancel, "cancel a purchase");

        await _periodLock.GuardAsync(purchase.InvoiceDate, "purchase", cancellationToken);

        // See InvoiceService.CancelAsync — the same double-move, in the other direction.
        if (await _debitNotes.HasLiveNotesForPurchaseAsync(purchase.Id, cancellationToken))
        {
            throw new ConflictException(
                $"Goods have already gone back on {purchase.PurchaseNumber}. Cancel the debit note first.",
                "PURCHASE_HAS_DEBIT_NOTES");
        }

        // Goods that never really came in have to leave the shelf again, and the reversal is
        // recorded as its own ledger row rather than by editing the original movement.
        foreach (var item in purchase.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is null)
            {
                continue;
            }

            await _stockLedger.RecordAsync(
                product,
                -item.Quantity,
                StockMovementType.PurchaseCancelled,
                purchase.Id,
                purchase.PurchaseNumber,
                notes: null,
                cancellationToken);
        }

        var supplier = await _supplierRepository.GetByIdAsync(purchase.SupplierId, cancellationToken);

        var allocations = await _paymentRepository.GetLiveAllocationsForPurchaseAsync(
            purchase.Id, cancellationToken);

        // Money handed over at the door goes back with the bill. Anything paid separately stays as
        // an advance on the supplier's account — see the same rule on the invoice side.
        foreach (var allocation in allocations.Where(a => a.Payment?.IsCounterPayment == true))
        {
            var payment = allocation.Payment!;
            payment.Supplier = supplier;

            await _paymentLedger.ReverseAsync(
                payment,
                PartyLedgerEntryType.PaymentCancelled,
                purchase.InvoiceDate,
                $"Paid on {purchase.PurchaseNumber}, which was cancelled",
                cancellationToken);
        }

        await _paymentLedger.ReleaseAllocationsForPurchaseAsync(
            purchase,
            allocations.Where(a => a.Payment?.IsCounterPayment != true).ToList(),
            cancellationToken);

        // See the note on InvoiceService.CancelAsync — AmountPaid keeps whatever the released
        // allocations left behind.
        purchase.BalanceDue = 0;

        if (supplier is not null)
        {
            await _partyLedger.RecordForSupplierAsync(
                supplier,
                -purchase.GrandTotal,
                PartyLedgerEntryType.PurchaseCancelled,
                purchase.InvoiceDate,
                purchase.Id,
                purchase.PurchaseNumber,
                notes: null,
                cancellationToken);
        }

        purchase.Status = PurchaseStatus.Cancelled;
        purchase.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(
            AuditAction.Cancelled,
            "Purchase",
            purchase.Id,
            purchase.PurchaseNumber,
            $"{purchase.SupplierName} {purchase.GrandTotal:0.00} dated {purchase.InvoiceDate:dd-MM-yyyy}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static PurchaseStatus? ParseStatus(string? status) =>
        Enum.TryParse<PurchaseStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;

    private static PaymentMode ParsePaymentMode(string? mode) =>
        Enum.TryParse<PaymentMode>(mode, ignoreCase: true, out var parsed) ? parsed : PaymentMode.Credit;
}
