using Application.DTOs.Products;

namespace Application.Interfaces;

/// <summary>
/// Loads a product catalogue from a spreadsheet.
/// <para>
/// Two calls, not one: <see cref="PreviewAsync"/> says what the file <i>would</i> do and writes
/// nothing, then <see cref="ImportAsync"/> does it. Both run the same validation, so a preview can
/// never promise something the import then refuses.
/// </para>
/// </summary>
public interface IProductImportService
{
    Task<ProductImportPreviewDto> PreviewAsync(
        ProductImportRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// All or nothing. A half-loaded catalogue is worse than an empty one — the shop cannot tell
    /// which parts are missing, and every stock figure from then on is suspect. If any row fails,
    /// the whole file is rejected and the user fixes it and uploads again.
    /// </summary>
    Task<ProductImportResultDto> ImportAsync(
        ProductImportRequest request, CancellationToken cancellationToken);
}
