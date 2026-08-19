using Application.Common.Exceptions;
using Application.DTOs.Payments;
using Application.Interfaces;
using Application.Mapping;

namespace Application.Services;

public class PartyAccountService : IPartyAccountService
{
    private readonly IPartyLedgerRepository _ledgerRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISupplierRepository _supplierRepository;

    private readonly IShopClock _clock;

    public PartyAccountService(
        IPartyLedgerRepository ledgerRepository,
        IPaymentRepository paymentRepository,
        ICustomerRepository customerRepository,
        ISupplierRepository supplierRepository,
        IShopClock clock)
    {
        _ledgerRepository = ledgerRepository;
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _clock = clock;
    }

    public async Task<PartyStatementDto> GetStatementAsync(
        Guid? customerId,
        Guid? supplierId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (partyId, partyName) = await ResolvePartyAsync(customerId, supplierId, cancellationToken);

        var (entries, totalCount, openingBalance, rangeMovement, carriedIn) =
            await _ledgerRepository.GetStatementAsync(
                customerId, supplierId, fromDate, toDate, page, pageSize, cancellationToken);

        // Opening plus everything that moved in the range. Deliberately not the last row's
        // BalanceAfter: that is the last row of this *page*, so a paged statement would report a
        // closing balance the customer does not owe.
        var closingBalance = openingBalance + rangeMovement;

        return new PartyStatementDto(
            partyId,
            partyName,
            openingBalance,
            closingBalance,
            fromDate,
            toDate,
            RunningBalance(entries, carriedIn),
            totalCount,
            page,
            pageSize);
    }

    public Task<IReadOnlyList<OpenDocumentDto>> GetOpenDocumentsAsync(
        Guid? customerId, Guid? supplierId, CancellationToken cancellationToken)
    {
        RequireExactlyOneParty(customerId, supplierId);

        return _paymentRepository.GetOpenDocumentsAsync(
            customerId, supplierId, _clock.Today, cancellationToken);
    }

    public async Task<CustomerAccountSummaryDto> GetCustomerAccountSummaryAsync(
        Guid customerId, CancellationToken cancellationToken) =>
        await _paymentRepository.GetCustomerAccountSummaryAsync(
            customerId, _clock.Today, cancellationToken)
        ?? throw new NotFoundException($"Customer {customerId} was not found.");

    /// <summary>
    /// Counts the balance down the page rather than serving the stored <c>BalanceAfter</c>. That
    /// column records what the account stood at when each row was <i>written</i>, which stops
    /// matching the page as soon as anything is entered out of date order — a bounce recorded today
    /// against a cheque banked with a later effective date does exactly that. A statement whose
    /// figures do not add up line by line is one the customer stops believing.
    /// </summary>
    private static IReadOnlyList<PartyLedgerEntryDto> RunningBalance(
        IReadOnlyList<Domain.Entities.PartyLedgerEntry> entries, decimal carriedIn)
    {
        var balance = carriedIn;
        var rows = new List<PartyLedgerEntryDto>(entries.Count);

        foreach (var entry in entries)
        {
            balance = Math.Round(balance + entry.Amount, 2, MidpointRounding.AwayFromZero);
            rows.Add(entry.ToDto() with { BalanceAfter = balance });
        }

        return rows;
    }

    private async Task<(Guid Id, string Name)> ResolvePartyAsync(
        Guid? customerId, Guid? supplierId, CancellationToken cancellationToken)
    {
        RequireExactlyOneParty(customerId, supplierId);

        if (customerId is { } id)
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Customer {id} was not found.");

            return (customer.Id, customer.Name);
        }

        var supplier = await _supplierRepository.GetByIdAsync(supplierId!.Value, cancellationToken)
            ?? throw new NotFoundException($"Supplier {supplierId} was not found.");

        return (supplier.Id, supplier.Name);
    }

    private static void RequireExactlyOneParty(Guid? customerId, Guid? supplierId)
    {
        if (customerId.HasValue == supplierId.HasValue)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["party"] =
                [
                    "A statement belongs to one party — pass either a customer or a supplier, not both.",
                ],
            });
        }
    }
}
