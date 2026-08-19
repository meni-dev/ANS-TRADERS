using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Payments;
using Application.Interfaces;
using Application.Mapping;
using Domain;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class ChequeService : IChequeService
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentLedger _paymentLedger;
    private readonly IPartyLedger _partyLedger;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly ICurrentUser _currentUser;

    public ChequeService(
        IPaymentRepository repository,
        IPaymentLedger paymentLedger,
        IPartyLedger partyLedger,
        ICustomerRepository customerRepository,
        ISupplierRepository supplierRepository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _paymentLedger = paymentLedger;
        _partyLedger = partyLedger;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PaymentListItemDto>> SearchAsync(
        ChequeListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchChequesAsync(
            ParseStatus(query.Status), query.FromDate, query.ToDate,
            query.Page, query.PageSize, cancellationToken);

        return new PagedResult<PaymentListItemDto>(
            items.Select(p => p.ToListItemDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public Task<PaymentDto> DepositAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken) =>
        MoveAsync(paymentId, ChequeStatus.Deposited, onDate, cancellationToken);

    public Task<PaymentDto> ClearAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken) =>
        MoveAsync(paymentId, ChequeStatus.Cleared, onDate, cancellationToken);

    public Task<PaymentDto> CancelAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken) =>
        MoveAsync(paymentId, ChequeStatus.Cancelled, onDate, cancellationToken);

    public async Task<PaymentDto> PostAsync(
        Guid paymentId, DateOnly onDate, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.PaymentRecord, "bank a cheque");

        var (payment, _) = await LoadChequeAsync(paymentId, cancellationToken);

        await LoadPartyAsync(payment, cancellationToken);
        await _paymentLedger.PostAsync(payment, onDate, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    public async Task<PaymentDto> BounceAsync(
        Guid paymentId, BounceChequeRequest request, CancellationToken cancellationToken)
    {
        // A bounce undoes a receipt that already moved a balance, so it belongs with cancellation.
        _currentUser.Require(Permission.PaymentCancel, "mark a cheque as bounced");

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Reason"] = ["Say what the bank gave as the reason"],
            });
        }

        var (payment, cheque) = await LoadChequeAsync(paymentId, cancellationToken);
        EnsureTransition(cheque, ChequeStatus.Bounced);

        // A cheque that was never recorded as deposited plainly was — the shop banked it and simply
        // did not tell the app. Stamping it beats refusing the bounce over missing bookkeeping.
        cheque.DepositedOn ??= request.BouncedOn;
        cheque.Status = ChequeStatus.Bounced;
        cheque.BouncedOn = request.BouncedOn;
        cheque.BounceReason = request.Reason.Trim();
        cheque.UpdatedAt = DateTimeOffset.UtcNow;

        await LoadPartyAsync(payment, cancellationToken);

        await _paymentLedger.ReverseAsync(
            payment,
            PartyLedgerEntryType.ChequeBounced,
            request.BouncedOn,
            $"Cheque {cheque.ChequeNumber} returned — {cheque.BounceReason}",
            cancellationToken);

        // The bank's charge is recovered from the party as its own ledger line, never as an invoice:
        // it is not a taxable supply, and minting an invoice number for it would put a hole in the
        // series the audit check watches.
        if (request.ChargeAmount is > 0)
        {
            var charge = Math.Round(request.ChargeAmount.Value, 2, MidpointRounding.AwayFromZero);
            var note = $"Bank charge on returned cheque {cheque.ChequeNumber}";

            if (payment.Customer is { } customer)
            {
                await _partyLedger.RecordForCustomerAsync(
                    customer, charge, PartyLedgerEntryType.ChequeBounceCharge, request.BouncedOn,
                    payment.Id, cheque.ChequeNumber, note, cancellationToken);
            }
            else if (payment.Supplier is { } supplier)
            {
                await _partyLedger.RecordForSupplierAsync(
                    supplier, charge, PartyLedgerEntryType.ChequeBounceCharge, request.BouncedOn,
                    payment.Id, cheque.ChequeNumber, note, cancellationToken);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    private async Task<PaymentDto> MoveAsync(
        Guid paymentId, ChequeStatus to, DateOnly onDate, CancellationToken cancellationToken)
    {
        // Cancelling a cheque takes money back off a party's account, so it sits with the other
        // reversals rather than with the day-to-day handling.
        _currentUser.Require(
            to == ChequeStatus.Cancelled ? Permission.PaymentCancel : Permission.PaymentRecord,
            to == ChequeStatus.Cancelled ? "cancel a cheque" : "move a cheque along");

        var (payment, cheque) = await LoadChequeAsync(paymentId, cancellationToken);
        EnsureTransition(cheque, to);

        cheque.Status = to;
        cheque.UpdatedAt = DateTimeOffset.UtcNow;

        switch (to)
        {
            case ChequeStatus.Deposited:
                cheque.DepositedOn = onDate;
                break;

            case ChequeStatus.Cleared:
                cheque.ClearedOn = onDate;

                // A post-dated cheque that reaches the bank without having been posted settles its
                // bills now, so the money lands in the month it actually arrived.
                if (payment.Status == PaymentStatus.Pending)
                {
                    await LoadPartyAsync(payment, cancellationToken);
                    await _paymentLedger.PostAsync(payment, onDate, cancellationToken);
                }

                break;

            case ChequeStatus.Cancelled:
                await LoadPartyAsync(payment, cancellationToken);
                await _paymentLedger.ReverseAsync(
                    payment, PartyLedgerEntryType.PaymentCancelled, onDate,
                    $"Cheque {cheque.ChequeNumber} handed back", cancellationToken);
                break;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    private async Task<(Payment Payment, ChequeDetail Cheque)> LoadChequeAsync(
        Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _repository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new NotFoundException($"Payment '{paymentId}' was not found", "PAYMENT_NOT_FOUND");

        var cheque = payment.Cheque
            ?? throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Mode"] = ["This payment was not made by cheque"],
            });

        return (payment, cheque);
    }

    /// <summary>
    /// The ledger moves a party's balance, so the party has to be loaded and tracked before it is
    /// asked to. The repository's paged search deliberately does not track.
    /// </summary>
    private async Task LoadPartyAsync(Payment payment, CancellationToken cancellationToken)
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

    private static void EnsureTransition(ChequeDetail cheque, ChequeStatus to)
    {
        if (ChequeTransitions.IsAllowed(cheque.Status, to))
        {
            return;
        }

        throw new ConflictException(
            $"A cheque that is {cheque.Status.ToString().ToLowerInvariant()} cannot be marked " +
            $"{to.ToString().ToLowerInvariant()}",
            "CHEQUE_TRANSITION_NOT_ALLOWED");
    }

    private static ChequeStatus? ParseStatus(string? status) =>
        Enum.TryParse<ChequeStatus>(status, ignoreCase: true, out var parsed) ? parsed : null;
}
