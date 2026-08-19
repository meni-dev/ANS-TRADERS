using System.Globalization;
using Application.Common.Exceptions;
using Application.DTOs.Products;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class ProductImportService : IProductImportService
{
    private readonly IProductRepository _repository;
    private readonly IStockLedger _stockLedger;
    private readonly IValidator<CreateProductRequest> _productValidator;
    private readonly ICurrentUser _currentUser;

    public ProductImportService(
        IProductRepository repository,
        IStockLedger stockLedger,
        IValidator<CreateProductRequest> productValidator,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _stockLedger = stockLedger;
        _productValidator = productValidator;
        _currentUser = currentUser;
    }

    // Preview reads nothing it does not already have permission to write, but it is gated all the
    // same: a preview of five thousand rows against the catalogue is a way to read the catalogue.
    public async Task<ProductImportPreviewDto> PreviewAsync(
        ProductImportRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ProductManage, "import a catalogue");

        var examined = await ExamineAsync(request, cancellationToken);

        return new ProductImportPreviewDto(
            examined.Count,
            examined.Count(r => r.Action == ImportRowAction.Create),
            examined.Count(r => r.Action == ImportRowAction.Update),
            examined.Count(r => r.Action == ImportRowAction.Reject),
            examined.Select(r => r.Result).ToList());
    }

    public async Task<ProductImportResultDto> ImportAsync(
        ProductImportRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.ProductManage, "import a catalogue");

        var examined = await ExamineAsync(request, cancellationToken);

        var rejected = examined.Where(r => r.Action == ImportRowAction.Reject).ToList();

        if (rejected.Count > 0)
        {
            // All or nothing. Letting the good rows through would leave the shop unable to say which
            // parts are on the master and which are not — and every stock figure after that is a
            // guess. Fix the file, upload again.
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Rows"] =
                [
                    $"{rejected.Count} of {examined.Count} rows have problems — nothing was imported. " +
                    "Fix them and upload again.",
                    .. rejected.Take(10).Select(r => $"Row {r.Result.RowNumber}: {string.Join("; ", r.Result.Errors)}"),
                ],
            });
        }

        var created = 0;
        var updated = 0;

        foreach (var row in examined)
        {
            if (row.Existing is { } existing)
            {
                Apply(existing, row.Parsed!);
                updated++;
                continue;
            }

            var product = new Product();
            Apply(product, row.Parsed!);
            await _repository.AddAsync(product, cancellationToken);

            // Opening stock enters through the ledger like every other movement, so an imported
            // product's history starts with a row explaining where its quantity came from.
            if (row.Parsed!.OpeningStock != 0)
            {
                await _stockLedger.RecordAsync(
                    product, row.Parsed.OpeningStock, StockMovementType.Opening,
                    null, "Catalogue import", notes: null, cancellationToken);
            }

            created++;
        }

        // One save for the whole file — the atomicity above is only real because of this.
        await _repository.SaveChangesAsync(cancellationToken);

        return new ProductImportResultDto(created, updated);
    }

    /// <summary>
    /// Parses and checks every row, without writing anything. Preview and import both come through
    /// here, so what the preview promised is exactly what the import does.
    /// </summary>
    private async Task<List<ExaminedRow>> ExamineAsync(
        ProductImportRequest request, CancellationToken cancellationToken)
    {
        var partNumbers = request.Rows
            .Select(r => Clean(r.PartNumber))
            .Where(p => p.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = partNumbers.Count == 0
            ? new Dictionary<string, Product>()
            : (Dictionary<string, Product>)await _repository.GetByPartNumbersAsync(
                partNumbers, cancellationToken);

        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var examined = new List<ExaminedRow>(request.Rows.Count);

        foreach (var row in request.Rows)
        {
            var errors = new List<string>();
            var partNumber = Clean(row.PartNumber);

            var parsed = Parse(row, errors);

            // Two rows claiming the same part number is a file the user has to fix — the database's
            // unique index would only tell them about the first collision, one upload at a time.
            if (partNumber.Length > 0)
            {
                if (seen.TryGetValue(partNumber, out var firstRow))
                {
                    errors.Add($"Part number '{partNumber}' is also on row {firstRow}");
                }
                else
                {
                    seen[partNumber] = row.RowNumber;
                }
            }

            Product? match = null;

            if (partNumber.Length > 0 && existing.TryGetValue(partNumber, out var found))
            {
                if (request.UpdateExisting)
                {
                    match = found;
                }
                else
                {
                    errors.Add($"Part number '{partNumber}' is already on the master");
                }
            }

            if (parsed is not null)
            {
                // The same validator the manual form runs, so the two paths can never drift apart.
                var result = await _productValidator.ValidateAsync(parsed, cancellationToken);
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
            }

            var action = errors.Count > 0
                ? ImportRowAction.Reject
                : match is not null ? ImportRowAction.Update : ImportRowAction.Create;

            examined.Add(new ExaminedRow(
                new ProductImportRowResult(
                    row.RowNumber, partNumber, Clean(row.ItemName), action.ToString(), errors),
                action,
                parsed,
                action == ImportRowAction.Update ? match : null));
        }

        return examined;
    }

    /// <summary>
    /// Turns the spreadsheet's strings into the same request the manual form builds. Anything that
    /// will not parse becomes an error naming the column, because "Purchase rate: '₹ 1,2S0' is not
    /// a number" is actionable and "input string was not in a correct format" is not.
    /// </summary>
    private static CreateProductRequest? Parse(ProductImportRow row, List<string> errors)
    {
        var gstRate = Number(row.GstRate, "GST rate", errors, required: true);
        var purchaseRate = Number(row.PurchaseRate, "Purchase rate", errors, required: true);
        var sellingRate = Number(row.SellingRate, "Selling rate", errors, required: true);
        var mrp = Number(row.Mrp, "MRP", errors, required: false);
        var openingStock = Number(row.OpeningStock, "Opening stock", errors, required: false);
        var reorderLevel = Number(row.ReorderLevel, "Reorder level", errors, required: false);

        if (gstRate is null || purchaseRate is null || sellingRate is null)
        {
            return null;
        }

        return new CreateProductRequest(
            Clean(row.ItemCode),
            Clean(row.PartNumber),
            Clean(row.ItemName),
            NullIfBlank(row.Description),
            NullIfBlank(row.VehicleBrand),
            NullIfBlank(row.VehicleModel),
            Clean(row.Hsn),
            gstRate.Value,
            Clean(row.Uqc),
            purchaseRate.Value,
            sellingRate.Value,
            mrp ?? 0m,
            openingStock ?? 0m,
            reorderLevel ?? 0m);
    }

    private static decimal? Number(string? raw, string column, List<string> errors, bool required)
    {
        // A spreadsheet writes money and percentages the way people do. Stripping the decoration is
        // not leniency — refusing "₹ 1,250" while accepting "1250" would just make the user do the
        // find-and-replace themselves.
        var text = (raw ?? string.Empty)
            .Replace("₹", string.Empty)
            .Replace(",", string.Empty)
            .Replace("%", string.Empty)
            .Trim();

        if (text.Length == 0)
        {
            if (required) errors.Add($"{column} is missing");
            return required ? null : 0m;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        errors.Add($"{column}: '{raw}' is not a number");
        return null;
    }

    private static void Apply(Product product, CreateProductRequest source)
    {
        product.ItemCode = source.ItemCode;
        product.PartNumber = source.PartNumber;
        product.ItemName = source.ItemName;
        product.Description = source.Description;
        product.VehicleBrand = source.VehicleBrand;
        product.VehicleModel = source.VehicleModel;
        product.Hsn = source.Hsn;
        product.GstRate = source.GstRate;
        product.CgstRate = Math.Round(source.GstRate / 2m, 2, MidpointRounding.AwayFromZero);
        product.SgstRate = source.GstRate - product.CgstRate;
        product.Uqc = source.Uqc;
        product.PurchaseRate = source.PurchaseRate;
        product.SellingRate = source.SellingRate;
        product.Mrp = source.Mrp;
        product.ReorderLevel = source.ReorderLevel;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        // OpeningStock is deliberately not applied on update: stock moves through the ledger, and
        // re-importing a price list must not silently rewrite what is on the shelf.
    }

    private static string Clean(string? value) => (value ?? string.Empty).Trim();

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ExaminedRow(
        ProductImportRowResult Result,
        ImportRowAction Action,
        CreateProductRequest? Parsed,
        Product? Existing);
}
