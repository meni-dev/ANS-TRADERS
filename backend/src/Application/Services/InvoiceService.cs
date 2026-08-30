using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Invoices;
using Application.DTOs.Payments;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockLedger _stockLedger;
    private readonly IShopSettingsRepository _shopSettings;
    private readonly IPaymentLedger _paymentLedger;
    private readonly IPartyLedger _partyLedger;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICreditNoteRepository _creditNotes;
    private readonly IPeriodLock _periodLock;
    private readonly ICashDayLock _cashDayLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateInvoiceRequest> _createValidator;

    private readonly IDocumentNumbers _numbers;

    private readonly IUnitOfWork _unitOfWork;

    public InvoiceService(
        IInvoiceRepository repository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IStockLedger stockLedger,
        IShopSettingsRepository shopSettings,
        IPaymentLedger paymentLedger,
        IPartyLedger partyLedger,
        IPaymentRepository paymentRepository,
        ICreditNoteRepository creditNotes,
        IPeriodLock periodLock,
        ICashDayLock cashDayLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IValidator<CreateInvoiceRequest> createValidator,
        IDocumentNumbers numbers,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _stockLedger = stockLedger;
        _shopSettings = shopSettings;
        _paymentLedger = paymentLedger;
        _partyLedger = partyLedger;
        _paymentRepository = paymentRepository;
        _creditNotes = creditNotes;
        _periodLock = periodLock;
        _cashDayLock = cashDayLock;
        _audit = audit;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _numbers = numbers;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<InvoiceListItemDto>> SearchAsync(
        InvoiceListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search,
            ParseStatus(query.Status),
            query.FromDate,
            query.ToDate,
            query.CustomerId,
            query.UnpaidOnly,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new PagedResult<InvoiceListItemDto>(
            items.Select(i => i.ToListItemDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<InvoiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{id}' was not found", "INVOICE_NOT_FOUND");

        return invoice.ToDto();
    }

    public async Task<InvoiceDto> CreateAsync(
        CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        // The document number is claimed inside this transaction, so a create that fails
        // rolls the number back with it rather than leaving a gap in the series.
        return await _unitOfWork.InTransactionAsync(async () =>
        {
            _currentUser.Require(Permission.BillCreate, "raise a bill");

            // Its own permission, checked only when a discount is actually being given: taking money off
            // a whole bill is the commonest way a counter leaks money, and plenty of shops want somebody
            // who can sell but cannot decide the price.
            if (request.BillDiscountPercent > 0 || request.BillDiscountAmount > 0)
            {
                _currentUser.Require(Permission.BillDiscount, "give a discount on the whole bill");
            }

            await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);
            await _periodLock.GuardAsync(request.InvoiceDate, "bill", cancellationToken);

            // Only the cash actually taken over the counter is at issue. A credit bill, or one
            // settled by UPI, changes nothing about what was in the drawer that day.
            await _cashDayLock.GuardAsync(
                request.InvoiceDate,
                "bill",
                request.AmountPaid > 0 && TendersCash(request.PaymentMode, request.TenderMode),
                cancellationToken);

            // A counter sale to somebody who is not on the books is the common case, so a null customer
            // is a supported shape rather than an error — the name typed on the form becomes the bill-to.
            Customer? customer = null;

            if (request.CustomerId is { } customerId)
            {
                customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
                    ?? throw new NotFoundException(
                        $"Customer '{customerId}' was not found", "CUSTOMER_NOT_FOUND");
            }

            var shop = await _shopSettings.GetAsync(cancellationToken);
            var isInterState = GstCalculator.IsInterState(shop.StateCode, customer?.StateCode);

            var financialYear = FinancialYear.For(request.InvoiceDate);
            var sequence = await _numbers.NextAsync(DocumentKind.Invoice, financialYear, cancellationToken);

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV/{financialYear}/{sequence:D4}",
                FinancialYear = financialYear,
                Sequence = sequence,
                InvoiceDate = request.InvoiceDate,
                CustomerId = customer?.Id,
                CustomerName = customer?.Name ?? request.WalkInName!.Trim(),
                CustomerPhone = customer?.Phone,
                CustomerGstin = customer?.Gstin,
                CustomerStateCode = customer?.StateCode,
                IsInterState = isInterState,
                PaymentMode = ParsePaymentMode(request.PaymentMode),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Status = InvoiceStatus.Issued,
            };

            // Two passes, not one. A bill-level discount cannot be split until every line is known, so
            // nothing is written until all of them have been checked — which also means a rejected line
            // leaves the shelf exactly as it was.
            var checkedLines = new List<(Product Product, CreateInvoiceItemRequest Line)>(request.Items.Count);

            foreach (var line in request.Items)
            {
                var product = await _productRepository.GetByIdAsync(line.ProductId, cancellationToken)
                    ?? throw new NotFoundException(
                        $"Product '{line.ProductId}' was not found", "PRODUCT_NOT_FOUND");

                if (!product.IsActive)
                {
                    throw new ValidationAppException(new Dictionary<string, string[]>
                    {
                        ["Items"] = [$"'{product.ItemName}' is inactive and cannot be billed"],
                    });
                }

                // A line discounted to nothing produces a tax invoice saying goods worth zero were
                // sold — while the goods themselves left the shelf. Giving something away is
                // ordinary at a counter, and the stock adjustment reasons already carry a
                // FreeIssue code built for exactly that: it moves the stock and calls it what it is.
                if (line.DiscountPercent >= 100m)
                {
                    throw new ValidationAppException(new Dictionary<string, string[]>
                    {
                        ["Items"] =
                        [
                            $"'{product.ItemName}' cannot be discounted to nothing on a bill. "
                            + "To give it away, adjust the stock with the reason 'Given free'.",
                        ],
                    });
                }

                // Selling a part above its printed MRP is not a pricing decision, it is an offence
                // under the Legal Metrology rules — so this blocks rather than warns.
                //
                // Zero means nobody has entered an MRP, not that the part may be given away. Treating it
                // as a real ceiling would refuse every line in a catalogue that has not been priced yet.
                if (product.Mrp > 0 && line.Rate > product.Mrp)
                {
                    throw new ValidationAppException(new Dictionary<string, string[]>
                    {
                        ["Items"] =
                        [
                            $"'{product.ItemName}' has an MRP of {product.Mrp:0.00} — it cannot be billed " +
                            $"at {line.Rate:0.00}",
                        ],
                    });
                }

                // Checked before anything is written: a bill is rejected whole rather than leaving
                // some lines applied and the shelf short.
                await _stockLedger.EnsureAvailableOnAsync(
                    product, line.Quantity, request.InvoiceDate, "bill", cancellationToken);

                checkedLines.Add((product, line));
            }

            // The counter enters either a percentage or "make it ₹950". The flat amount wins when both
            // arrive, because it is the more specific instruction.
            var billDiscount = request.BillDiscountAmount > 0
                ? Money(request.BillDiscountAmount)
                : Money(checkedLines.Sum(c => GstCalculator
                    .ComputeLine(c.Line.Quantity, c.Line.Rate, c.Line.DiscountPercent, c.Product.GstRate, isInterState)
                    .TaxableAmount) * request.BillDiscountPercent / 100m);

            // The GST rate comes from the product master, never from the request — see the same note in
            // PurchaseService.
            var lineAmounts = GstCalculator.ApplyBillDiscount(
                checkedLines
                    .Select(c => (c.Line.Quantity, c.Line.Rate, c.Line.DiscountPercent, c.Product.GstRate))
                    .ToList(),
                billDiscount,
                isInterState);

            if (billDiscount > 0 && lineAmounts.Any(l => l.TaxableAmount < 0))
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["BillDiscountAmount"] = ["That discount is more than the bill is worth"],
                });
            }

            invoice.BillDiscountPercent = request.BillDiscountPercent;
            invoice.BillDiscountAmount = billDiscount;

            for (var i = 0; i < checkedLines.Count; i++)
            {
                var (product, line) = checkedLines[i];
                var amounts = lineAmounts[i];

                invoice.Items.Add(new InvoiceItem
                {
                    InvoiceId = invoice.Id,
                    ProductId = product.Id,
                    PartNumber = product.PartNumber,
                    ItemName = product.ItemName,
                    Hsn = product.Hsn,
                    Uqc = product.Uqc,
                    Quantity = line.Quantity,
                    Rate = line.Rate,

                    // Frozen here and never recomputed — see the note on InvoiceItem.CostRate.
                    CostRate = product.PurchaseRate,
                    DiscountPercent = line.DiscountPercent,
                    DiscountAmount = amounts.DiscountAmount,
                    BillDiscountShare = amounts.BillDiscountShare,
                    GrossAmount = amounts.GrossAmount,
                    TaxableAmount = amounts.TaxableAmount,
                    GstRate = product.GstRate,
                    // Snapshotted beside the rate: a part reclassified next year must not move
                    // last year's bill into a different table of a return already filed.
                    SupplyType = product.SupplyType,
                    CgstAmount = amounts.CgstAmount,
                    SgstAmount = amounts.SgstAmount,
                    IgstAmount = amounts.IgstAmount,
                    LineTotal = amounts.LineTotal,
                });

                await _stockLedger.RecordAsync(
                    product,
                    -line.Quantity,
                    StockMovementType.Sale,
                    invoice.InvoiceDate,
                    invoice.Id,
                    invoice.InvoiceNumber,
                    notes: null,
                    cancellationToken);
            }

            var totals = GstCalculator.ComputeDocument(lineAmounts);

            invoice.ItemCount = invoice.Items.Count;
            invoice.SubTotal = totals.SubTotal;
            invoice.DiscountAmount = totals.DiscountAmount;
            invoice.TaxableAmount = totals.TaxableAmount;
            invoice.CgstAmount = totals.CgstAmount;
            invoice.SgstAmount = totals.SgstAmount;
            invoice.IgstAmount = totals.IgstAmount;
            invoice.TotalTax = totals.TotalTax;
            invoice.RoundOff = totals.RoundOff;
            invoice.GrandTotal = totals.GrandTotal;

            // The bill is born unpaid. Anything collected at the counter arrives below as a payment, so
            // that every rupee the shop takes has a row behind it and "what did we collect today" is one
            // question with one answer.
            invoice.AmountPaid = 0;
            invoice.BalanceDue = invoice.GrandTotal;

            // Due on issue unless the customer has agreed terms. Ageing then measures lateness rather
            // than mere age, so a customer on 30 days is not called overdue on day one.
            invoice.DueDate = customer is { CreditDays: > 0 } onTerms
                ? request.InvoiceDate.AddDays(onTerms.CreditDays)
                : request.InvoiceDate;

            await _repository.AddAsync(invoice, cancellationToken);

            // Read before the ledger moves it. The credit-limit check further down needs what the
            // customer owed *coming in*, and RecordForCustomerAsync is about to add this bill to the
            // running balance — read it afterwards and the bill counts twice, which refused every
            // customer at roughly half the limit the shop had set for them.
            var owedBeforeThisBill = customer?.OutstandingBalance ?? 0m;

            if (customer is not null)
            {
                await _partyLedger.RecordForCustomerAsync(
                    customer,
                    invoice.GrandTotal,
                    PartyLedgerEntryType.Invoice,
                    invoice.InvoiceDate,
                    invoice.Id,
                    invoice.InvoiceNumber,
                    notes: null,
                    cancellationToken);
            }

            // Cash and card sales are settled in full at the counter, so the tender is the bill.
            // A request saying otherwise is refused rather than quietly rewritten: accepting a
            // number and recording a different one tells the caller its figure was fine when it
            // was not.
            // A rupee of slack, not exact equality. The counter screen works the total out for
            // itself so the operator can see it before saving, and the server works it out again;
            // the two can land a paisa apart on round-off. This is here to catch a figure that is
            // not the bill at all, not to argue about the last coin.
            // Whatever route it took — every line free, or a bill discount that swallowed the lot —
            // a tax invoice for nothing is not a sale. The goods still moved, so this is caught
            // rather than allowed to sit in the register and in GSTR-1.
            if (invoice.GrandTotal <= 0)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["Items"] =
                    [
                        "This bill comes to nothing. A tax invoice records a sale — to give goods "
                        + "away, adjust the stock with the reason 'Given free'.",
                    ],
                });
            }

            if (invoice.PaymentMode != PaymentMode.Credit
                && request.AmountPaid != 0
                && Math.Abs(request.AmountPaid - invoice.GrandTotal) > 1m)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["AmountPaid"] =
                    [
                        $"A {invoice.PaymentMode} sale is settled in full — {invoice.GrandTotal:0.00}. "
                        + "Use Credit to take part payment.",
                    ],
                });
            }

            var tender = invoice.PaymentMode == PaymentMode.Credit
                ? request.AmountPaid
                : invoice.GrandTotal;

            if (tender > invoice.GrandTotal)
            {
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["AmountPaid"] = [$"Cannot exceed the invoice total of {invoice.GrandTotal:0.00}"],
                });
            }

            // What this bill leaves on the customer's account, against the limit the shop set for
            // them. Zero means no limit — an unset field is not an instruction to refuse every
            // credit sale.
            //
            // Checked here rather than up front because it needs the finished total: the discount
            // and the tax are what decide whether the limit is actually crossed.
            if (customer is { CreditLimit: > 0 } && invoice.GrandTotal - tender > 0)
            {
                var wouldOwe = owedBeforeThisBill + (invoice.GrandTotal - tender);

                if (wouldOwe > customer.CreditLimit)
                {
                    throw new ValidationAppException(new Dictionary<string, string[]>
                    {
                        ["CustomerId"] =
                        [
                            $"{customer.Name} would owe {wouldOwe:0.00} against a credit limit of "
                            + $"{customer.CreditLimit:0.00}. Take payment now, or raise the limit.",
                        ],
                    });
                }
            }

            if (tender > 0)
            {
                // A credit bill part-paid at the counter was still tendered as something, and "Credit"
                // is not a tender — hence a separate mode for how the money actually arrived.
                var tenderMode = invoice.PaymentMode == PaymentMode.Credit
                    ? ParsePaymentMode(request.TenderMode, PaymentMode.Cash)
                    : invoice.PaymentMode;

                await _paymentLedger.RecordCounterPaymentAsync(
                    new PaymentDraft(
                        PaymentDirection.Received,
                        customer,
                        Supplier: null,
                        invoice.CustomerName,
                        invoice.InvoiceDate,
                        tender,
                        tenderMode,
                        request.TenderReference,
                        Notes: null,
                        IsCounterPayment: true,
                        request.Cheque is { } cheque
                            ? new ChequeDraft(
                                cheque.ChequeNumber,
                                cheque.BankName,
                                cheque.ChequeDate,
                                cheque.ReceivedOn ?? invoice.InvoiceDate)
                            : null,
                        [new AllocationTarget(invoice, null, tender)]),
                    cancellationToken);
            }

            // A discount on the whole bill is the one thing on this screen that hands money away
            // without goods moving, which is exactly why it has a permission of its own. Logging the
            // permission and not the use of it was half a control.
            if (invoice.BillDiscountAmount > 0)
            {
                await _audit.RecordAsync(
                    AuditAction.DiscountGiven,
                    "Invoice",
                    invoice.Id,
                    invoice.InvoiceNumber,
                    $"{invoice.BillDiscountAmount:0.00} off {invoice.SubTotal:0.00} for {invoice.CustomerName}",
                    cancellationToken);
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return invoice.ToDto();
        }, cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{id}' was not found", "INVOICE_NOT_FOUND");

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new ConflictException(
                $"Invoice '{invoice.InvoiceNumber}' is already cancelled", "INVOICE_ALREADY_CANCELLED");
        }

        _currentUser.Require(Permission.BillCancel, "cancel a bill");

        // The bill's own date, not today's — cancelling a March bill in June changes March.
        await _periodLock.GuardUndoAsync(invoice.InvoiceDate, "bill", cancellationToken);

        // Cancelling puts every line back on the shelf, and a credit note has already put some of
        // them there. Allowing both would return the same goods twice — and the customer has a note
        // in hand crediting a bill that would no longer exist.
        if (await _creditNotes.HasLiveNotesForInvoiceAsync(invoice.Id, cancellationToken))
        {
            throw new ConflictException(
                $"Goods have already come back on {invoice.InvoiceNumber}. Cancel the credit note first.",
                "INVOICE_HAS_CREDIT_NOTES");
        }

        // Goods that were never really sold go back on the shelf, as a fresh ledger row rather
        // than by editing the original movement.
        foreach (var item in invoice.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is null)
            {
                continue;
            }

            await _stockLedger.RecordAsync(
                product,
                item.Quantity,
                StockMovementType.SaleCancelled,
                invoice.InvoiceDate,
                invoice.Id,
                invoice.InvoiceNumber,
                notes: null,
                cancellationToken);
        }

        var customer = invoice.CustomerId is { } customerId
            ? await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            : null;

        var allocations = await _paymentRepository.GetLiveAllocationsForInvoiceAsync(
            invoice.Id, cancellationToken);

        // A counter payment belongs to this bill and goes back over the counter with it. A receipt
        // the customer walked in with is different money — it stays, and becomes an advance on their
        // account ready for the re-issued bill.
        foreach (var allocation in allocations.Where(a => a.Payment?.IsCounterPayment == true))
        {
            var payment = allocation.Payment!;
            payment.Customer = customer;

            await _paymentLedger.ReverseAsync(
                payment,
                PartyLedgerEntryType.PaymentCancelled,
                invoice.InvoiceDate,
                $"Collected on {invoice.InvoiceNumber}, which was cancelled",
                cancellationToken);
        }

        await _paymentLedger.ReleaseAllocationsForInvoiceAsync(
            invoice,
            allocations.Where(a => a.Payment?.IsCounterPayment != true).ToList(),
            cancellationToken);

        // A cancelled document owes nothing. Leaving a balance behind is why every query in the app
        // has to remember to filter on status — this stops the next one inheriting that.
        //
        // AmountPaid is left as the allocations left it: releasing them already brought it back to
        // what is genuinely still applied, and forcing it to zero would erase the record of money
        // that really did cross the counter.
        invoice.BalanceDue = 0;

        if (customer is not null)
        {
            await _partyLedger.RecordForCustomerAsync(
                customer,
                -invoice.GrandTotal,
                PartyLedgerEntryType.InvoiceCancelled,
                invoice.InvoiceDate,
                invoice.Id,
                invoice.InvoiceNumber,
                notes: null,
                cancellationToken);
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;

        await _audit.RecordAsync(
            AuditAction.Cancelled,
            "Invoice",
            invoice.Id,
            invoice.InvoiceNumber,
            $"{invoice.CustomerName} {invoice.GrandTotal:0.00} dated {invoice.InvoiceDate:dd-MM-yyyy}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static decimal Money(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Whether the money taken at the counter was notes and coins. A credit bill part-paid at the
    /// counter tenders as something else, so the tender mode decides — see the note where the
    /// counter payment is recorded.
    /// </summary>
    private static bool TendersCash(string? paymentMode, string? tenderMode)
    {
        var mode = ParsePaymentMode(paymentMode);
        return mode == PaymentMode.Credit
            ? ParsePaymentMode(tenderMode, PaymentMode.Cash) == PaymentMode.Cash
            : mode == PaymentMode.Cash;
    }

    private static PaymentMode ParsePaymentMode(string? mode, PaymentMode fallback) =>
        Enum.TryParse<PaymentMode>(mode, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static InvoiceStatus? ParseStatus(string? status) =>
        Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;

    private static PaymentMode ParsePaymentMode(string? mode) =>
        Enum.TryParse<PaymentMode>(mode, ignoreCase: true, out var parsed) ? parsed : PaymentMode.Cash;
}
