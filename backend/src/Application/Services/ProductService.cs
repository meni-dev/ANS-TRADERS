using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Products;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IStockLedger _stockLedger;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;

    public ProductService(
        IProductRepository repository,
        IStockLedger stockLedger,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _stockLedger = stockLedger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Whether this caller sees buying prices. Managing the catalogue means typing the rates in, so
    /// that permission carries the right to read them without needing the cost one as well.
    /// </summary>
    private bool ShowCost =>
        _currentUser.Has(Permission.CostView) || _currentUser.Has(Permission.ProductManage);

    public async Task<PagedResult<ProductDto>> SearchAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.Search, query.ActiveOnly, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<ProductDto>(
            items.Select(p => p.ToDto(ShowCost)).ToList(), totalCount, query.Page, query.PageSize);
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found", "PRODUCT_NOT_FOUND");

        return product.ToDto(ShowCost);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ProductManage, "add a part to the catalogue");

        await ValidateAsync(_createValidator, request, cancellationToken);

        if (await _repository.PartNumberExistsAsync(request.PartNumber, null, cancellationToken))
        {
            throw new ConflictException(
                $"Part number '{request.PartNumber}' already exists", "DUPLICATE_PART_NUMBER");
        }

        var product = new Product
        {
            ItemCode = request.ItemCode,
            PartNumber = request.PartNumber,
            ItemName = request.ItemName,
            Description = request.Description,
            VehicleBrand = request.VehicleBrand,
            VehicleModel = request.VehicleModel,
            Hsn = request.Hsn,
            GstRate = request.GstRate,
            CgstRate = HalfOf(request.GstRate),
            SgstRate = HalfOf(request.GstRate),
            Uqc = request.Uqc,
            PurchaseRate = request.PurchaseRate,
            SellingRate = request.SellingRate,
            Mrp = request.Mrp,
            OpeningStock = request.OpeningStock,
            ReorderLevel = request.ReorderLevel,
        };

        await _repository.AddAsync(product, cancellationToken);

        // Opening stock enters through the ledger like everything else, so the very first row of a
        // product's history explains where its quantity came from.
        if (request.OpeningStock != 0)
        {
            await _stockLedger.RecordAsync(
                product,
                request.OpeningStock,
                StockMovementType.Opening,
                referenceId: null,
                referenceNumber: null,
                notes: "Opening stock",
                cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ProductManage, "change a part");

        await ValidateAsync(_updateValidator, request, cancellationToken);

        var product = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found", "PRODUCT_NOT_FOUND");

        if (await _repository.PartNumberExistsAsync(request.PartNumber, id, cancellationToken))
        {
            throw new ConflictException(
                $"Part number '{request.PartNumber}' already exists", "DUPLICATE_PART_NUMBER");
        }

        product.ItemCode = request.ItemCode;
        product.PartNumber = request.PartNumber;
        product.ItemName = request.ItemName;
        product.Description = request.Description;
        product.VehicleBrand = request.VehicleBrand;
        product.VehicleModel = request.VehicleModel;
        product.Hsn = request.Hsn;
        product.GstRate = request.GstRate;
        product.CgstRate = HalfOf(request.GstRate);
        product.SgstRate = HalfOf(request.GstRate);
        product.Uqc = request.Uqc;
        product.PurchaseRate = request.PurchaseRate;
        product.SellingRate = request.SellingRate;
        product.Mrp = request.Mrp;
        product.ReorderLevel = request.ReorderLevel;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ProductManage, "retire a part");

        var product = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found", "PRODUCT_NOT_FOUND");

        product.IsActive = false;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ProductManage, "bring a part back");

        var product = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Product '{id}' was not found", "PRODUCT_NOT_FOUND");

        product.IsActive = true;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationAppException(errors);
        }
    }

    /// <summary>
    /// CGST and SGST are always an even split of the GST rate. They are derived here rather than
    /// accepted from the request so the three values can never drift out of sync.
    /// </summary>
    private static decimal HalfOf(decimal gstRate) => Math.Round(gstRate / 2m, 2);
}
