using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Payments;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentLedger _paymentLedger;
    private readonly IPartyLedger _partyLedger;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IPeriodLock _periodLock;
    private readonly IAuditLog _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreatePaymentRequest> _createValidator;

    public PaymentService(
        IPaymentRepository repository,
        IPaymentLedger paymentLedger,
        IPartyLedger partyLedger,
        ICustomerRepository customerRepository,
        ISupplierRepository supplierRepository,
        IPeriodLock periodLock,
        IAuditLog audit,
        ICurrentUser currentUser,
        IValidator<CreatePaymentRequest> createValidator)
    {
        _repository = repository;
        _paymentLedger = paymentLedger;
        _partyLedger = partyLedger;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _periodLock = periodLock;
        _audit = audit;
        _currentUser = currentUser;
        _createValidator = createValidator;
    }

    public async Task<PagedResult<PaymentListItemDto>> SearchAsync(
        PaymentListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search,
            Parse<PaymentDirection>(query.Direction),
            Parse<PaymentStatus>(query.Status),
            Parse<PaymentMode>(query.Mode),
            query.CustomerId,
            query.SupplierId,
            query.FromDate,
            query.ToDate,
            query.UnallocatedOnly,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new PagedResult<PaymentListItemDto>(
            items.Select(p => p.ToListItemDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<PaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Payment '{id}' was not found", "PAYMENT_NOT_FOUND");

        return payment.ToDto();
    }

    public async Task<PaymentDto> CreateAsync(
        CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PaymentRecord, "record a payment");

        await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);
        await _periodLock.GuardAsync(request.PaymentDate, "receipt", cancellationToken);

        var direction = Parse<PaymentDirection>(request.Direction)
            ?? throw Invalid("Direction", $"'{request.Direction}' is not a direction this app knows");

        var mode = Parse<PaymentMode>(request.Mode)
            ?? throw Invalid("Mode", $"'{request.Mode}' is not a payment mode this app knows");

        var (customer, supplier) = await LoadPartyAsync(direction, request, cancellationToken);

        var targets = await ResolveTargetsAsync(direction, request, customer, supplier, cancellationToken);

        var draft = new PaymentDraft(
            direction,
            customer,
            supplier,
            customer?.Name ?? supplier?.Name ?? request.WalkInName!.Trim(),
            request.PaymentDate,
            request.Amount,
            mode,
            request.ReferenceNumber,
            request.Notes,
            IsCounterPayment: false,
            request.Cheque is { } cheque
                ? new ChequeDraft(
                    cheque.ChequeNumber,
                    cheque.BankName,
                    cheque.ChequeDate,
                    cheque.ReceivedOn ?? request.PaymentDate)
                : null,
            targets);

        var payment = await _paymentLedger.ReceiveAsync(draft, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    public async Task<PaymentDto> AllocateAsync(
        Guid id, AllocatePaymentRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PaymentRecord, "settle a bill against a receipt");

        var payment = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Payment '{id}' was not found", "PAYMENT_NOT_FOUND");

        var targets = await ResolveDocumentsAsync(
            payment.Direction, request.Allocations, payment.CustomerId, payment.SupplierId, cancellationToken);

        await _paymentLedger.AllocateAsync(payment, targets, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PaymentCancel, "cancel a payment");

        var payment = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Payment '{id}' was not found", "PAYMENT_NOT_FOUND");

        // A tender taken across the counter belongs to its document. Cancelling it on its own would
        // leave an invoice claiming to be paid with nothing behind it.
        if (payment.IsCounterPayment)
        {
            var document = payment.Allocations.FirstOrDefault()?.DocumentNumber;

            throw new ConflictException(
                document is null
                    ? "This was collected while raising a document — cancel that document instead"
                    : $"This was collected on {document} — cancel {document} instead",
                "PAYMENT_BELONGS_TO_DOCUMENT");
        }

        await LoadTrackedPartyAsync(payment, cancellationToken);

        await _paymentLedger.ReverseAsync(
            payment, PartyLedgerEntryType.PaymentCancelled, payment.PaymentDate,
            "Payment cancelled", cancellationToken);

        await _audit.RecordAsync(
            AuditAction.Cancelled,
            "Payment",
            payment.Id,
            payment.ReceiptNumber,
            $"{payment.PartyName} {payment.Amount:0.00} dated {payment.PaymentDate:dd-MM-yyyy}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentSummaryDto> GetSummaryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        // Money actually in, on the day it was effective. A post-dated cheque is deliberately absent
        // — it is Pending, and counting it would say the shop holds cash it cannot spend.
        var (posted, _) = await _repository.SearchAsync(
            null, null, PaymentStatus.Posted, null, null, null, fromDate, toDate, null,
            page: 1, pageSize: int.MaxValue, cancellationToken);

        // Cheques still settling, regardless of when they were taken — what matters is that they
        // have not turned into money yet.
        var (pendingCheques, _) = await _repository.SearchChequesAsync(
            null, null, null, page: 1, pageSize: int.MaxValue, cancellationToken);

        var inHand = pendingCheques
            .Where(p => p.Cheque is not null && Domain.ChequeTransitions.IsOutstanding(p.Cheque.Status))
            .ToList();

        var collected = posted.Where(p => p.Direction == PaymentDirection.Received).Sum(p => p.Amount);
        var paidOut = posted.Where(p => p.Direction == PaymentDirection.Paid).Sum(p => p.Amount);

        var byMode = posted
            .GroupBy(p => p.Mode)
            .Select(g => new PaymentModeTotalDto(
                g.Key.ToString(),
                ModeLabel(g.Key),
                Round(g.Where(p => p.Direction == PaymentDirection.Received).Sum(p => p.Amount)),
                Round(g.Where(p => p.Direction == PaymentDirection.Paid).Sum(p => p.Amount)),
                g.Count()))
            .OrderByDescending(m => m.Received)
            .ToList();

        return new PaymentSummaryDto(
            Round(collected),
            Round(paidOut),
            Round(collected - paidOut),
            Round(inHand.Sum(p => p.Amount)),
            inHand.Count,
            posted.Count,
            byMode);
    }

    public async Task<DuesSummaryDto> GetDuesAsync(CancellationToken cancellationToken)
    {
        var (customers, _) = await _customerRepository.SearchAsync(
            null, null, page: 1, pageSize: int.MaxValue, cancellationToken);

        var (suppliers, _) = await _supplierRepository.SearchAsync(
            null, null, page: 1, pageSize: int.MaxValue, cancellationToken);

        // Only positive balances are owed. Netting one customer's advance against another's debt
        // would understate what is actually out there, so advances are reported separately.
        var owedByCustomers = customers.Where(c => c.OutstandingBalance > 0).ToList();
        var owedToSuppliers = suppliers.Where(s => s.OutstandingBalance > 0).ToList();

        var advances = customers.Where(c => c.OutstandingBalance < 0).Sum(c => -c.OutstandingBalance);

        return new DuesSummaryDto(
            Round(owedByCustomers.Sum(c => c.OutstandingBalance)),
            Round(owedToSuppliers.Sum(s => s.OutstandingBalance)),
            Round(advances),
            owedByCustomers.Count,
            owedToSuppliers.Count);
    }

    public async Task AdjustAsync(AdjustPartyBalanceRequest request, CancellationToken cancellationToken)
    {
        // Moving a party's balance by hand is the strongest thing on this screen — it changes what
        // somebody owes without any document behind it.
        _currentUser.Require(Permission.PaymentCancel, "adjust a party's balance by hand");

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw Invalid("Reason", "Say why the balance is being corrected");
        }

        if (request.Amount == 0)
        {
            throw Invalid("Amount", "An adjustment of zero would change nothing");
        }

        if (request.CustomerId is null == (request.SupplierId is null))
        {
            throw Invalid("Party", "Pick either a customer or a supplier");
        }

        if (request.CustomerId is { } customerId)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
                ?? throw new NotFoundException($"Customer '{customerId}' was not found", "CUSTOMER_NOT_FOUND");

            await _partyLedger.RecordForCustomerAsync(
                customer, request.Amount, PartyLedgerEntryType.Adjustment,
                DateOnly.FromDateTime(DateTime.UtcNow), null, null, request.Reason.Trim(), cancellationToken);
        }
        else
        {
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId!.Value, cancellationToken)
                ?? throw new NotFoundException(
                    $"Supplier '{request.SupplierId}' was not found", "SUPPLIER_NOT_FOUND");

            await _partyLedger.RecordForSupplierAsync(
                supplier, request.Amount, PartyLedgerEntryType.Adjustment,
                DateOnly.FromDateTime(DateTime.UtcNow), null, null, request.Reason.Trim(), cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Customer? Customer, Supplier? Supplier)> LoadPartyAsync(
        PaymentDirection direction, CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (direction == PaymentDirection.Received)
        {
            if (request.SupplierId is not null)
            {
                throw Invalid("SupplierId", "Money received comes from a customer, not a supplier");
            }

            if (request.CustomerId is not { } customerId)
            {
                // A walk-in paying off nothing in particular is unusual but legal — the money still
                // has to land in the cash book.
                return (null, null);
            }

            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
                ?? throw new NotFoundException($"Customer '{customerId}' was not found", "CUSTOMER_NOT_FOUND");

            return (customer, null);
        }

        // Money out to a customer is a refund — of a credit note, or of an advance they no longer
        // want sitting on their account. Legal, and it belongs in the cash book like any other
        // payment; it was only ever refused because until returns existed nothing could produce it.
        if (request.CustomerId is { } refundTo)
        {
            var refundee = await _customerRepository.GetByIdAsync(refundTo, cancellationToken)
                ?? throw new NotFoundException($"Customer '{refundTo}' was not found", "CUSTOMER_NOT_FOUND");

            return (refundee, null);
        }

        if (request.SupplierId is not { } id)
        {
            throw Invalid("SupplierId", "Pick the supplier being paid");
        }

        var supplier = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Supplier '{id}' was not found", "SUPPLIER_NOT_FOUND");

        return (null, supplier);
    }

    /// <summary>
    /// Turns the request's allocations into tracked documents — either the ones the user named, or,
    /// when they asked for it, the party's open bills oldest first.
    /// </summary>
    private async Task<IReadOnlyList<AllocationTarget>> ResolveTargetsAsync(
        PaymentDirection direction,
        CreatePaymentRequest request,
        Customer? customer,
        Supplier? supplier,
        CancellationToken cancellationToken)
    {
        if (request.Allocations.Count > 0)
        {
            return await ResolveDocumentsAsync(
                direction, request.Allocations, customer?.Id, supplier?.Id, cancellationToken);
        }

        if (!request.AutoAllocateOldestFirst)
        {
            return [];
        }

        if (direction == PaymentDirection.Received)
        {
            if (customer is null)
            {
                return [];
            }

            var invoices = await _repository.GetOpenInvoicesForCustomerAsync(customer.Id, cancellationToken);

            var plan = PaymentAllocationPlanner.Plan(
                request.Amount,
                invoices.Select(i => new OpenDocument(i.Id, i.InvoiceDate, i.BalanceDue)));

            return plan
                .Select(p => new AllocationTarget(invoices.First(i => i.Id == p.DocumentId), null, p.Amount))
                .ToList();
        }

        if (supplier is null)
        {
            return [];
        }

        var purchases = await _repository.GetOpenPurchasesForSupplierAsync(supplier.Id, cancellationToken);

        var purchasePlan = PaymentAllocationPlanner.Plan(
            request.Amount,
            purchases.Select(p => new OpenDocument(p.Id, p.InvoiceDate, p.BalanceDue)));

        return purchasePlan
            .Select(p => new AllocationTarget(null, purchases.First(x => x.Id == p.DocumentId), p.Amount))
            .ToList();
    }

    /// <summary>
    /// Loads each named document and checks it can legally take the money — right direction, not
    /// cancelled, belongs to the same party, and not more than it still owes.
    /// </summary>
    private async Task<IReadOnlyList<AllocationTarget>> ResolveDocumentsAsync(
        PaymentDirection direction,
        IReadOnlyList<AllocationRequest> allocations,
        Guid? customerId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        if (allocations.Select(a => a.DocumentId).Distinct().Count() != allocations.Count)
        {
            throw Invalid("Allocations", "The same document appears more than once");
        }

        var targets = new List<AllocationTarget>(allocations.Count);

        var openInvoices = direction == PaymentDirection.Received && customerId is { } cid
            ? await _repository.GetOpenInvoicesForCustomerAsync(cid, cancellationToken)
            : [];

        var openPurchases = direction == PaymentDirection.Paid && supplierId is { } sid
            ? await _repository.GetOpenPurchasesForSupplierAsync(sid, cancellationToken)
            : [];

        foreach (var allocation in allocations)
        {
            if (direction == PaymentDirection.Received)
            {
                var invoice = openInvoices.FirstOrDefault(i => i.Id == allocation.DocumentId)
                    ?? throw Invalid(
                        "Allocations",
                        "That invoice is not an open bill for this customer");

                EnsureWithinOutstanding(
                    allocation.Amount, invoice.GrandTotal - invoice.AmountPaid, invoice.InvoiceNumber);

                targets.Add(new AllocationTarget(invoice, null, allocation.Amount));
            }
            else
            {
                var purchase = openPurchases.FirstOrDefault(p => p.Id == allocation.DocumentId)
                    ?? throw Invalid(
                        "Allocations",
                        "That bill is not an open purchase for this supplier");

                EnsureWithinOutstanding(
                    allocation.Amount, purchase.GrandTotal - purchase.AmountPaid, purchase.PurchaseNumber);

                targets.Add(new AllocationTarget(null, purchase, allocation.Amount));
            }
        }

        return targets;
    }

    private static void EnsureWithinOutstanding(decimal amount, decimal outstanding, string documentNumber)
    {
        if (amount > Round(outstanding))
        {
            throw Invalid(
                "Allocations",
                $"{documentNumber} only has {outstanding:0.00} outstanding");
        }
    }

    private async Task LoadTrackedPartyAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Customer is null && payment.CustomerId is { } customerId)
        {
            payment.Customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        }

        if (payment.Supplier is null && payment.SupplierId is { } supplierId)
        {
            payment.Supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        }
    }

    private static ValidationAppException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static TEnum? Parse<TEnum>(string? value) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : null;

    /// <summary>The enum name is for the database; nobody at a counter says "Upi" or "BankTransfer".</summary>
    public static string ModeLabel(PaymentMode mode) => mode switch
    {
        PaymentMode.Upi => "UPI",
        PaymentMode.BankTransfer => "Bank transfer",
        _ => mode.ToString(),
    };

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
