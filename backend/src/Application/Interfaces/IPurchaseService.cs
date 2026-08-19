using Application.Common;
using Application.DTOs.Purchases;

namespace Application.Interfaces;

public interface IPurchaseService
{
    Task<PagedResult<PurchaseListItemDto>> SearchAsync(
        PurchaseListQuery query, CancellationToken cancellationToken);

    Task<PurchaseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PurchaseDto> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken);

    /// <summary>Voids the document. Recorded purchases are never deleted — see <c>PurchaseStatus</c>.</summary>
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
}
