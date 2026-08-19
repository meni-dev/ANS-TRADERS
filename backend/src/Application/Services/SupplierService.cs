using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Suppliers;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using FluentValidation;

namespace Application.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _repository;
    private readonly IPartyLedger _partyLedger;
    private readonly IValidator<CreateSupplierRequest> _createValidator;
    private readonly IValidator<UpdateSupplierRequest> _updateValidator;

    private readonly IShopClock _clock;

    public SupplierService(
        ISupplierRepository repository,
        IPartyLedger partyLedger,
        IValidator<CreateSupplierRequest> createValidator,
        IValidator<UpdateSupplierRequest> updateValidator,
        IShopClock clock)
    {
        _repository = repository;
        _partyLedger = partyLedger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _clock = clock;
    }

    public async Task<PagedResult<SupplierDto>> SearchAsync(SupplierListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search, query.ActiveOnly, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<SupplierDto>(
            items.Select(s => s.ToDto()).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Supplier '{id}' was not found", "SUPPLIER_NOT_FOUND");

        return supplier.ToDto();
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_createValidator, request, cancellationToken);

        if (await _repository.PhoneExistsAsync(request.Phone, null, cancellationToken))
        {
            throw new ConflictException(
                $"A supplier with phone '{request.Phone}' already exists", "DUPLICATE_PHONE");
        }

        var supplier = new Supplier
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = Nullify(request.Email),
            Gstin = Nullify(request.Gstin),
            ContactPerson = Nullify(request.ContactPerson),
            AddressLine1 = Nullify(request.AddressLine1),
            AddressLine2 = Nullify(request.AddressLine2),
            City = Nullify(request.City),
            State = Nullify(request.State),
            StateCode = Nullify(request.StateCode),
            Pincode = Nullify(request.Pincode),
            PaymentTerms = Nullify(request.PaymentTerms),
            OpeningBalance = request.OpeningBalance,
        };

        await _repository.AddAsync(supplier, cancellationToken);

        if (request.OpeningBalance != 0)
        {
            // See the note on CustomerService — the same dead figure, on the other side of the book.
            await _partyLedger.RecordForSupplierAsync(
                supplier,
                request.OpeningBalance,
                Domain.Enums.PartyLedgerEntryType.Opening,
                _clock.Today,
                referenceId: null,
                referenceNumber: null,
                notes: "Balance brought forward",
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return supplier.ToDto();
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        await ValidationHelper.ValidateAsync(_updateValidator, request, cancellationToken);

        var supplier = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Supplier '{id}' was not found", "SUPPLIER_NOT_FOUND");

        if (await _repository.PhoneExistsAsync(request.Phone, id, cancellationToken))
        {
            throw new ConflictException(
                $"A supplier with phone '{request.Phone}' already exists", "DUPLICATE_PHONE");
        }

        supplier.Name = request.Name;
        supplier.Phone = request.Phone;
        supplier.Email = Nullify(request.Email);
        supplier.Gstin = Nullify(request.Gstin);
        supplier.ContactPerson = Nullify(request.ContactPerson);
        supplier.AddressLine1 = Nullify(request.AddressLine1);
        supplier.AddressLine2 = Nullify(request.AddressLine2);
        supplier.City = Nullify(request.City);
        supplier.State = Nullify(request.State);
        supplier.StateCode = Nullify(request.StateCode);
        supplier.Pincode = Nullify(request.Pincode);
        supplier.PaymentTerms = Nullify(request.PaymentTerms);
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        return supplier.ToDto();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Supplier '{id}' was not found", "SUPPLIER_NOT_FOUND");

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Supplier '{id}' was not found", "SUPPLIER_NOT_FOUND");

        supplier.IsActive = true;
        supplier.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The web form submits untouched optional inputs as empty strings. Storing those as NULL
    /// keeps "no GSTIN" a single value in the database instead of two.
    /// </summary>
    private static string? Nullify(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
