using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Customers;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using FluentValidation;

namespace Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IPartyLedger _partyLedger;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    private readonly IShopClock _clock;

    public CustomerService(
        ICustomerRepository repository,
        IPartyLedger partyLedger,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator,
        IShopClock clock)
    {
        _repository = repository;
        _partyLedger = partyLedger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _clock = clock;
    }

    public async Task<PagedResult<CustomerDto>> SearchAsync(CustomerListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search, query.ActiveOnly, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<CustomerDto>(
            items.Select(c => c.ToDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found", "CUSTOMER_NOT_FOUND");

        return customer.ToDto();
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);

        if (await _repository.PhoneExistsAsync(request.Phone, null, cancellationToken))
        {
            throw new ConflictException(
                $"A customer with phone '{request.Phone}' already exists", "DUPLICATE_PHONE");
        }

        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = Nullify(request.Email),
            Gstin = Nullify(request.Gstin),
            AddressLine1 = Nullify(request.AddressLine1),
            AddressLine2 = Nullify(request.AddressLine2),
            City = Nullify(request.City),
            State = Nullify(request.State),
            StateCode = Nullify(request.StateCode),
            Pincode = Nullify(request.Pincode),
            CreditLimit = request.CreditLimit,
            CreditDays = request.CreditDays,
            OpeningBalance = request.OpeningBalance,
        };

        await _repository.AddAsync(customer, cancellationToken);

        if (request.OpeningBalance != 0)
        {
            // What they already owed on the day they were written into the book. Without this the
            // figure would be recorded on the master and read by nothing — which is exactly what it
            // used to be, and why a customer could carry a balance the app never showed.
            await _partyLedger.RecordForCustomerAsync(
                customer,
                request.OpeningBalance,
                Domain.Enums.PartyLedgerEntryType.Opening,
                _clock.Today,
                referenceId: null,
                referenceNumber: null,
                notes: "Balance brought forward",
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updateValidator, request, cancellationToken);

        var customer = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found", "CUSTOMER_NOT_FOUND");

        if (await _repository.PhoneExistsAsync(request.Phone, id, cancellationToken))
        {
            throw new ConflictException(
                $"A customer with phone '{request.Phone}' already exists", "DUPLICATE_PHONE");
        }

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Email = Nullify(request.Email);
        customer.Gstin = Nullify(request.Gstin);
        customer.AddressLine1 = Nullify(request.AddressLine1);
        customer.AddressLine2 = Nullify(request.AddressLine2);
        customer.City = Nullify(request.City);
        customer.State = Nullify(request.State);
        customer.StateCode = Nullify(request.StateCode);
        customer.Pincode = Nullify(request.Pincode);
        customer.CreditLimit = request.CreditLimit;
        customer.CreditDays = request.CreditDays;
        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        return customer.ToDto();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found", "CUSTOMER_NOT_FOUND");

        customer.IsActive = false;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found", "CUSTOMER_NOT_FOUND");

        customer.IsActive = true;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The web form submits untouched optional inputs as empty strings. Storing those as NULL
    /// keeps "no GSTIN" a single value in the database instead of two.
    /// </summary>
    private static string? Nullify(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
