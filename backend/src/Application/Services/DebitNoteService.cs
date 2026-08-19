using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Returns;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class DebitNoteService : IDebitNoteService
{
    private readonly IDebitNoteRepository _repository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedger _stockLedger;
    private readonly IPartyLedger _partyLedger;
    private readonly IPaymentLedger _paymentLedger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPeriodLock _periodLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateDebitNoteRequest> _createValidator;

    public DebitNoteService(
        IDebitNoteRepository repository,
        IPurchaseRepository purchaseRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IStockLedger stockLedger,
        IPartyLedger partyLedger,
        IPaymentLedger paymentLedger,
        IPaymentRepository paymentRepository,
        IPeriodLock periodLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IValidator<CreateDebitNoteRequest> createValidator)
    {
        _repository = repository;
        _purchaseRepository = purchaseRepository;
        _supplierRepository = supplierRepository;
        _productRepository = productRepository;
        _stockLedger = stockLedger;
        _partyLedger = partyLedger;
        _paymentLedger = paymentLedger;
        _paymentRepository = paymentRepository;
        _periodLock = periodLock;
        _audit = audit;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    public async Task<PagedResult<DebitNoteListItemDto>> SearchAsync(
        DebitNoteListQuery query, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PurchaseView, "see purchase returns");

        var (items, totalCount) = await _repository.SearchAsync(
            query.Search, query.SupplierId, query.PurchaseId, query.FromDate, query.ToDate,
            query.Page, query.PageSize, cancellationToken);

        return new PagedResult<DebitNoteListItemDto>(
            items.Select(n => n.ToListItemDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<DebitNoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit note '{id}' was not found", "DEBIT_NOTE_NOT_FOUND"))
        .ToDto();

    public async Task<DebitNoteDto> CreateAsync(
        CreateDebitNoteRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PurchaseReturn, "send goods back to a supplier");

        await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);
        await _periodLock.GuardAsync(request.NoteDate, "debit note", cancellationToken);

        var purchase = await _purchaseRepository.GetByIdAsync(request.PurchaseId, cancellationToken)
            ?? throw new NotFoundException(
                $"Purchase '{request.PurchaseId}' was not found", "INVOICE_NOT_FOUND");

        if (purchase.Status == PurchaseStatus.Cancelled)
        {
            throw new ConflictException(
                $"{purchase.PurchaseNumber} was cancelled — there is nothing left to credit",
                "INVOICE_CANCELLED");
        }

        if (request.NoteDate < purchase.InvoiceDate)
        {
            throw Invalid("NoteDate", $"Goods cannot come back before {purchase.PurchaseNumber} was raised");
        }

        // Tracked: the party ledger moves the supplier's balance on the entity itself.
        // Required, unlike a credit note's customer: a purchase always has a supplier on file, so
        // there is no walk-in case here and no account for a credit to fall through.
        var supplier = await _supplierRepository.GetByIdAsync(purchase.SupplierId, cancellationToken)
            ?? throw new NotFoundException(
                $"Supplier '{purchase.SupplierId}' was not found", "SUPPLIER_NOT_FOUND");

        // Everything is checked before one field is written. The EnsureAvailable doctrine: a bad
        // request is rejected whole rather than half-applied, so nothing has to be unwound.
        var lines = ResolveLines(purchase, request.Lines);

        // Goods leaving the shelf are checked before one field is written, exactly as a sale is.
        foreach (var (item, quantity) in lines)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is not null)
            {
                _stockLedger.EnsureAvailable(product, quantity, "send back");
            }
        }

        var financialYear = FinancialYear.For(request.NoteDate);
        var sequence = await _repository.GetLastSequenceAsync(financialYear, cancellationToken) + 1;

        var note = new DebitNote
        {
            DebitNoteNumber = $"DBN/{financialYear}/{sequence:D4}",
            FinancialYear = financialYear,
            Sequence = sequence,
            NoteDate = request.NoteDate,
            PurchaseId = purchase.Id,
            PurchaseNumber = purchase.PurchaseNumber,
            PurchaseDate = purchase.InvoiceDate,
            SupplierId = purchase.SupplierId,
            SupplierName = purchase.SupplierName,
            SupplierGstin = purchase.SupplierGstin,
            SupplierStateCode = purchase.SupplierStateCode,

            // Copied, never recomputed. If the supplier's state code was corrected after the sale,
            // this note still has to reverse the tax that was actually charged.
            IsInterState = purchase.IsInterState,
            Reason = request.Reason.Trim(),
        };

        var lineAmounts = new List<LineAmounts>(lines.Count);

        foreach (var (item, quantity) in lines)
        {
            var amounts = GstCalculator.ComputeLine(
                quantity, item.Rate, item.DiscountPercent, item.GstRate, purchase.IsInterState);

            lineAmounts.Add(amounts);

            note.Items.Add(new DebitNoteItem
            {
                DebitNoteId = note.Id,
                PurchaseItemId = item.Id,
                ProductId = item.ProductId,

                // From the purchase line, not the product master: if the part was renamed since the
                // sale, this note must still read as the bill it credits.
                PartNumber = item.PartNumber,
                ItemName = item.ItemName,
                Hsn = item.Hsn,
                Uqc = item.Uqc,
                Quantity = quantity,
                Rate = item.Rate,
                DiscountPercent = item.DiscountPercent,
                DiscountAmount = amounts.DiscountAmount,
                GrossAmount = amounts.GrossAmount,
                TaxableAmount = amounts.TaxableAmount,
                GstRate = item.GstRate,
                CgstAmount = amounts.CgstAmount,
                SgstAmount = amounts.SgstAmount,
                IgstAmount = amounts.IgstAmount,
                LineTotal = amounts.LineTotal,
            });

            item.ReturnedQuantity += quantity;

            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is not null)
            {
                // Negative, and checked first. This is the one place the purchase side is not a
                // mirror of the sales side: a sales return puts goods back on the shelf, a purchase
                // return takes them off it — and you cannot send back what you have already sold.
                await _stockLedger.RecordAsync(
                    product, -quantity, StockMovementType.PurchaseReturn,
                    note.Id, note.DebitNoteNumber, notes: null, cancellationToken);
            }
        }

        ApplyTotals(note, GstCalculator.ComputeDocument(lineAmounts));

        // Against the bill first, capped at what it still owed; the rest belongs to the account.
        note.AppliedToPurchaseAmount = _paymentLedger.ApplyDebit(purchase, note.GrandTotal);

        var refund = Round(request.RefundAmount ?? 0m);
        var creditToAccount = Round(note.GrandTotal - note.AppliedToPurchaseAmount);

        if (refund > creditToAccount)
        {
            // The shop can only hand back money it actually took. The part that closed the bill was
            // never cash in the drawer.
            throw Invalid(
                "RefundAmount",
                creditToAccount <= 0
                    ? $"{note.DebitNoteNumber} went entirely against {purchase.PurchaseNumber}, so there is nothing to refund"
                    : $"Only {creditToAccount:0.00} of this credit can be refunded");
        }

        await _repository.AddAsync(note, cancellationToken);

        // The whole note, always. The split above is only how the settlement is presented on the
        // document; what the shop no longer owes is the full value of the goods it sent back.
        await _partyLedger.RecordForSupplierAsync(
            supplier, -note.GrandTotal, PartyLedgerEntryType.DebitNote, note.NoteDate,
            note.Id, note.DebitNoteNumber, note.Reason, cancellationToken);

        if (refund > 0)
        {
            await _paymentLedger.RecordCounterPaymentAsync(
                new PaymentDraft(
                    // Money coming back IN from the supplier, so Received — the mirror of a customer
                    // refund, which goes out.
                    PaymentDirection.Received,
                    null,
                    supplier,
                    note.SupplierName,
                    note.NoteDate,
                    refund,
                    ParseMode(request.RefundMode),
                    request.RefundReference,
                    $"Refund against {note.DebitNoteNumber}",
                    IsCounterPayment: true,
                    Cheque: null,
                    Allocations: [new AllocationTarget(null, null, refund) { DebitNote = note }]),
                cancellationToken);

            note.RefundedAmount = refund;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }

    public async Task<ReturnableDocumentDto> GetReturnableAsync(
        Guid purchaseId, CancellationToken cancellationToken)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new NotFoundException($"Purchase '{purchaseId}' was not found", "INVOICE_NOT_FOUND");

        var cancelled = purchase.Status == PurchaseStatus.Cancelled;

        var lines = purchase.Items
            .Select(i => new ReturnableLineDto(
                i.Id, i.ProductId, i.PartNumber, i.ItemName, i.Uqc,
                i.Quantity, i.ReturnedQuantity, i.Quantity - i.ReturnedQuantity,
                i.Rate, i.DiscountPercent, i.GstRate))
            .ToList();

        return new ReturnableDocumentDto(
            purchase.Id,
            purchase.PurchaseNumber,
            purchase.InvoiceDate,
            purchase.SupplierName,
            purchase.IsInterState,
            !cancelled && lines.Any(l => l.QuantityReturnable > 0),
            cancelled
                ? "This bill was cancelled — the sale never stood, so there is nothing to return."
                : lines.All(l => l.QuantityReturnable <= 0)
                    ? "Everything on this bill has already come back."
                    : null,
            lines);
    }

    /// <summary>
    /// Matches each requested line to the purchase line it reverses and checks it can still come
    /// back. Every failure is collected rather than thrown on the first, so the counter sees
    /// everything wrong with the request at once instead of one problem per attempt.
    /// </summary>
    private static List<(PurchaseItem Item, decimal Quantity)> ResolveLines(
        Purchase purchase, IReadOnlyList<ReturnLineRequest> requested)
    {
        var errors = new List<string>();
        var resolved = new List<(PurchaseItem, decimal)>();
        var seen = new HashSet<Guid>();

        foreach (var line in requested)
        {
            if (line.Quantity <= 0)
            {
                continue;
            }

            if (!seen.Add(line.DocumentItemId))
            {
                errors.Add("The same line appears more than once");
                continue;
            }

            var item = purchase.Items.FirstOrDefault(i => i.Id == line.DocumentItemId);

            if (item is null)
            {
                errors.Add($"One of the lines is not on {purchase.PurchaseNumber}");
                continue;
            }

            var returnable = item.Quantity - item.ReturnedQuantity;

            if (line.Quantity > returnable)
            {
                // Says how many can still go back, which is the question actually being asked.
                errors.Add(returnable <= 0
                    ? $"{item.ItemName} has already been returned in full"
                    : $"{item.ItemName}: only {returnable:0.###} of {item.Quantity:0.###} can still be returned");

                continue;
            }

            resolved.Add((item, line.Quantity));
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]> { ["Lines"] = [.. errors] });
        }

        if (resolved.Count == 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Lines"] = ["Enter how much of at least one line is coming back"],
            });
        }

        return resolved;
    }

    private static void ApplyTotals(DebitNote note, DocumentAmounts amounts)
    {
        note.ItemCount = note.Items.Count;
        note.SubTotal = amounts.SubTotal;
        note.DiscountAmount = amounts.DiscountAmount;
        note.TaxableAmount = amounts.TaxableAmount;
        note.CgstAmount = amounts.CgstAmount;
        note.SgstAmount = amounts.SgstAmount;
        note.IgstAmount = amounts.IgstAmount;
        note.TotalTax = amounts.TotalTax;
        note.RoundOff = amounts.RoundOff;
        note.GrandTotal = amounts.GrandTotal;
    }

    private static PaymentMode ParseMode(string? mode) =>
        Enum.TryParse<PaymentMode>(mode, ignoreCase: true, out var parsed) && parsed != PaymentMode.Credit
            ? parsed
            : PaymentMode.Cash;

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static ValidationAppException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>
    /// Undoes a credit note keyed in error. Everything it did is put back and the row survives with
    /// its number — mirroring <c>PurchaseService.CancelAsync</c>, because a document that existed
    /// must stay traceable even once it is void.
    /// </summary>
    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit note '{id}' was not found", "DEBIT_NOTE_NOT_FOUND");

        if (note.Status == DebitNoteStatus.Cancelled)
        {
            throw new ConflictException(
                $"{note.DebitNoteNumber} is already cancelled", "DEBIT_NOTE_ALREADY_CANCELLED");
        }

        var purchase = await _purchaseRepository.GetByIdAsync(note.PurchaseId, cancellationToken)
            ?? throw new NotFoundException(
                $"Purchase '{note.PurchaseId}' was not found", "INVOICE_NOT_FOUND");

        var supplier = await _supplierRepository.GetByIdAsync(note.SupplierId, cancellationToken)
            ?? throw new NotFoundException(
                $"Supplier '{note.SupplierId}' was not found", "SUPPLIER_NOT_FOUND");

        // The cash first: if the shop handed money back against this note, that has to come back in
        // before anything else moves, and it is reversed rather than deleted.
        foreach (var allocation in
                 await _paymentRepository.GetLiveAllocationsForDebitNoteAsync(note.Id, cancellationToken))
        {
            var payment = allocation.Payment!;
            payment.Supplier = supplier;

            await _paymentLedger.ReverseAsync(
                payment,
                PartyLedgerEntryType.PaymentCancelled,
                note.NoteDate,
                $"Refunded on {note.DebitNoteNumber}, which was cancelled",
                cancellationToken);
        }

        foreach (var line in note.Items)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, cancellationToken);

            if (product is not null)
            {
                await _stockLedger.RecordAsync(
                    product, line.Quantity, StockMovementType.PurchaseReturnCancelled,
                    note.Id, note.DebitNoteNumber, notes: null, cancellationToken);
            }

            var item = purchase.Items.FirstOrDefault(i => i.Id == line.PurchaseItemId);

            if (item is not null)
            {
                item.ReturnedQuantity -= line.Quantity;
            }
        }

        // The bill goes back to owing what it owed, which may re-open one that this note had closed.
        // That is correct: the debt was never actually settled.
        _paymentLedger.ReleaseDebit(purchase, note.AppliedToPurchaseAmount);

        if (supplier is not null)
        {
            await _partyLedger.RecordForSupplierAsync(
                supplier, note.GrandTotal, PartyLedgerEntryType.DebitNoteCancelled, note.NoteDate,
                note.Id, note.DebitNoteNumber, "Credit note cancelled", cancellationToken);
        }

        // AppliedToPurchaseAmount is deliberately left standing — the same precedent as AmountPaid on
        // a cancelled bill: it is the record of what this document once did. Every reconciliation
        // query therefore filters on status rather than expecting it to be zero.
        _currentUser.Require(Permission.PurchaseReturn, "cancel a debit note");

        note.Status = DebitNoteStatus.Cancelled;
        note.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(
            AuditAction.Cancelled,
            "DebitNote",
            note.Id,
            note.DebitNoteNumber,
            $"{note.SupplierName} {note.GrandTotal:0.00} against {note.PurchaseNumber}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
