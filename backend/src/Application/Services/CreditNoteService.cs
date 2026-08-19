using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Returns;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class CreditNoteService : ICreditNoteService
{
    private readonly ICreditNoteRepository _repository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedger _stockLedger;
    private readonly IPartyLedger _partyLedger;
    private readonly IPaymentLedger _paymentLedger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPeriodLock _periodLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateCreditNoteRequest> _createValidator;

    public CreditNoteService(
        ICreditNoteRepository repository,
        IInvoiceRepository invoiceRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IStockLedger stockLedger,
        IPartyLedger partyLedger,
        IPaymentLedger paymentLedger,
        IPaymentRepository paymentRepository,
        IPeriodLock periodLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IValidator<CreateCreditNoteRequest> createValidator)
    {
        _repository = repository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
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

    public async Task<PagedResult<CreditNoteListItemDto>> SearchAsync(
        CreditNoteListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search, query.CustomerId, query.InvoiceId, query.FromDate, query.ToDate,
            query.Page, query.PageSize, cancellationToken);

        return new PagedResult<CreditNoteListItemDto>(
            items.Select(n => n.ToListItemDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<CreditNoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit note '{id}' was not found", "CREDIT_NOTE_NOT_FOUND"))
        .ToDto();

    public async Task<CreditNoteDto> CreateAsync(
        CreateCreditNoteRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.SalesReturn, "take goods back");

        await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);
        await _periodLock.GuardAsync(request.NoteDate, "credit note", cancellationToken);

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(
                $"Invoice '{request.InvoiceId}' was not found", "INVOICE_NOT_FOUND");

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new ConflictException(
                $"{invoice.InvoiceNumber} was cancelled — there is nothing left to credit",
                "INVOICE_CANCELLED");
        }

        if (request.NoteDate < invoice.InvoiceDate)
        {
            throw Invalid("NoteDate", $"Goods cannot come back before {invoice.InvoiceNumber} was raised");
        }

        // Tracked: the party ledger moves the customer's balance on the entity itself.
        var customer = invoice.CustomerId is { } customerId
            ? await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            : null;

        // Everything is checked before one field is written. The EnsureAvailable doctrine: a bad
        // request is rejected whole rather than half-applied, so nothing has to be unwound.
        var lines = ResolveLines(invoice, request.Lines);

        var financialYear = FinancialYear.For(request.NoteDate);
        var sequence = await _repository.GetLastSequenceAsync(financialYear, cancellationToken) + 1;

        var note = new CreditNote
        {
            CreditNoteNumber = $"CRN/{financialYear}/{sequence:D4}",
            FinancialYear = financialYear,
            Sequence = sequence,
            NoteDate = request.NoteDate,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            CustomerPhone = invoice.CustomerPhone,
            CustomerGstin = invoice.CustomerGstin,
            CustomerStateCode = invoice.CustomerStateCode,

            // Copied, never recomputed. If the customer's state code was corrected after the sale,
            // this note still has to reverse the tax that was actually charged.
            IsInterState = invoice.IsInterState,
            Reason = request.Reason.Trim(),
        };

        var lineAmounts = new List<LineAmounts>(lines.Count);

        foreach (var (item, quantity) in lines)
        {
            // The bill-level discount came off this line too, so a return has to give back what was
            // actually charged. Apportioned by how much of the line is coming back — return half of
            // it and half the discount goes with it. Without this a discounted bill returned in full
            // would credit more than the customer ever paid.
            var shareOfBillDiscount = item.Quantity == 0
                ? 0m
                : Math.Round(
                    item.BillDiscountShare * quantity / item.Quantity, 2, MidpointRounding.AwayFromZero);

            var amounts = GstCalculator.ComputeLine(
                quantity, item.Rate, item.DiscountPercent, item.GstRate, invoice.IsInterState,
                shareOfBillDiscount);

            lineAmounts.Add(amounts);

            note.Items.Add(new CreditNoteItem
            {
                CreditNoteId = note.Id,
                InvoiceItemId = item.Id,
                ProductId = item.ProductId,

                // From the invoice line, not the product master: if the part was renamed since the
                // sale, this note must still read as the bill it credits.
                PartNumber = item.PartNumber,
                ItemName = item.ItemName,
                Hsn = item.Hsn,
                Uqc = item.Uqc,
                Quantity = quantity,
                Rate = item.Rate,
                CostRate = item.CostRate,
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
                await _stockLedger.RecordAsync(
                    product, quantity, StockMovementType.SalesReturn,
                    note.Id, note.CreditNoteNumber, notes: null, cancellationToken);
            }
        }

        ApplyTotals(note, GstCalculator.ComputeDocument(lineAmounts));

        // Against the bill first, capped at what it still owed; the rest belongs to the account.
        note.AppliedToInvoiceAmount = _paymentLedger.ApplyCredit(invoice, note.GrandTotal);

        var refund = Round(request.RefundAmount ?? 0m);
        var creditToAccount = Round(note.GrandTotal - note.AppliedToInvoiceAmount);

        if (refund > creditToAccount)
        {
            // The shop can only hand back money it actually took. The part that closed the bill was
            // never cash in the drawer.
            throw Invalid(
                "RefundAmount",
                creditToAccount <= 0
                    ? $"{note.CreditNoteNumber} went entirely against {invoice.InvoiceNumber}, so there is nothing to refund"
                    : $"Only {creditToAccount:0.00} of this credit can be refunded");
        }

        // A walk-in has no account for a credit to sit on. Refusing beats letting the money quietly
        // disappear — and quietly overstating what the shop earned.
        if (customer is null && creditToAccount > refund)
        {
            throw Invalid(
                "RefundAmount",
                $"{invoice.InvoiceNumber} was a counter sale with no customer on file. " +
                $"Refund the full {creditToAccount:0.00}, or re-bill it to a saved customer first.");
        }

        await _repository.AddAsync(note, cancellationToken);

        if (customer is not null)
        {
            // The whole note, always. The split above is only how the settlement is presented on the
            // document; what the customer is owed is the full value of the goods they brought back.
            await _partyLedger.RecordForCustomerAsync(
                customer, -note.GrandTotal, PartyLedgerEntryType.CreditNote, note.NoteDate,
                note.Id, note.CreditNoteNumber, note.Reason, cancellationToken);
        }

        if (refund > 0)
        {
            await _paymentLedger.RecordCounterPaymentAsync(
                new PaymentDraft(
                    PaymentDirection.Paid,
                    customer,
                    null,
                    note.CustomerName,
                    note.NoteDate,
                    refund,
                    ParseMode(request.RefundMode),
                    request.RefundReference,
                    $"Refund against {note.CreditNoteNumber}",
                    IsCounterPayment: true,
                    Cheque: null,
                    Allocations: [new AllocationTarget(null, null, refund) { CreditNote = note }]),
                cancellationToken);

            note.RefundedAmount = refund;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return note.ToDto();
    }

    public async Task<ReturnableDocumentDto> GetReturnableAsync(
        Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{invoiceId}' was not found", "INVOICE_NOT_FOUND");

        var cancelled = invoice.Status == InvoiceStatus.Cancelled;

        var lines = invoice.Items
            .Select(i => new ReturnableLineDto(
                i.Id, i.ProductId, i.PartNumber, i.ItemName, i.Uqc,
                i.Quantity, i.ReturnedQuantity, i.Quantity - i.ReturnedQuantity,
                i.Rate, i.DiscountPercent, i.GstRate))
            .ToList();

        return new ReturnableDocumentDto(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.CustomerName,
            invoice.IsInterState,
            !cancelled && lines.Any(l => l.QuantityReturnable > 0),
            cancelled
                ? "This bill was cancelled — the sale never stood, so there is nothing to return."
                : lines.All(l => l.QuantityReturnable <= 0)
                    ? "Everything on this bill has already come back."
                    : null,
            lines);
    }

    /// <summary>
    /// Matches each requested line to the invoice line it reverses and checks it can still come
    /// back. Every failure is collected rather than thrown on the first, so the counter sees
    /// everything wrong with the request at once instead of one problem per attempt.
    /// </summary>
    private static List<(InvoiceItem Item, decimal Quantity)> ResolveLines(
        Invoice invoice, IReadOnlyList<ReturnLineRequest> requested)
    {
        var errors = new List<string>();
        var resolved = new List<(InvoiceItem, decimal)>();
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

            var item = invoice.Items.FirstOrDefault(i => i.Id == line.DocumentItemId);

            if (item is null)
            {
                errors.Add($"One of the lines is not on {invoice.InvoiceNumber}");
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

    private static void ApplyTotals(CreditNote note, DocumentAmounts amounts)
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
    /// its number — mirroring <c>InvoiceService.CancelAsync</c>, because a document that existed
    /// must stay traceable even once it is void.
    /// </summary>
    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var note = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Credit note '{id}' was not found", "CREDIT_NOTE_NOT_FOUND");

        if (note.Status == CreditNoteStatus.Cancelled)
        {
            throw new ConflictException(
                $"{note.CreditNoteNumber} is already cancelled", "CREDIT_NOTE_ALREADY_CANCELLED");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(note.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(
                $"Invoice '{note.InvoiceId}' was not found", "INVOICE_NOT_FOUND");

        var customer = note.CustomerId is { } customerId
            ? await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            : null;

        // The cash first: if the shop handed money back against this note, that has to come back in
        // before anything else moves, and it is reversed rather than deleted.
        foreach (var allocation in
                 await _paymentRepository.GetLiveAllocationsForCreditNoteAsync(note.Id, cancellationToken))
        {
            var payment = allocation.Payment!;
            payment.Customer = customer;

            await _paymentLedger.ReverseAsync(
                payment,
                PartyLedgerEntryType.PaymentCancelled,
                note.NoteDate,
                $"Refunded on {note.CreditNoteNumber}, which was cancelled",
                cancellationToken);
        }

        foreach (var line in note.Items)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, cancellationToken);

            if (product is not null)
            {
                await _stockLedger.RecordAsync(
                    product, -line.Quantity, StockMovementType.SalesReturnCancelled,
                    note.Id, note.CreditNoteNumber, notes: null, cancellationToken);
            }

            var item = invoice.Items.FirstOrDefault(i => i.Id == line.InvoiceItemId);

            if (item is not null)
            {
                item.ReturnedQuantity -= line.Quantity;
            }
        }

        // The bill goes back to owing what it owed, which may re-open one that this note had closed.
        // That is correct: the debt was never actually settled.
        _paymentLedger.ReleaseCredit(invoice, note.AppliedToInvoiceAmount);

        if (customer is not null)
        {
            await _partyLedger.RecordForCustomerAsync(
                customer, note.GrandTotal, PartyLedgerEntryType.CreditNoteCancelled, note.NoteDate,
                note.Id, note.CreditNoteNumber, "Credit note cancelled", cancellationToken);
        }

        // AppliedToInvoiceAmount is deliberately left standing — the same precedent as AmountPaid on
        // a cancelled bill: it is the record of what this document once did. Every reconciliation
        // query therefore filters on status rather than expecting it to be zero.
        _currentUser.Require(Permission.SalesReturn, "cancel a credit note");

        note.Status = CreditNoteStatus.Cancelled;
        note.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(
            AuditAction.Cancelled,
            "CreditNote",
            note.Id,
            note.CreditNoteNumber,
            $"{note.CustomerName} {note.GrandTotal:0.00} against {note.InvoiceNumber}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
