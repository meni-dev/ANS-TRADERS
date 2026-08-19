using Application.Common;
using Application.DTOs.Payments;

namespace Application.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentListItemDto>> SearchAsync(
        PaymentListQuery query, CancellationToken cancellationToken);

    Task<PaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PaymentDto> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken);

    /// <summary>Spends money already on account against further documents.</summary>
    Task<PaymentDto> AllocateAsync(
        Guid id, AllocatePaymentRequest request, CancellationToken cancellationToken);

    /// <summary>Keyed in error. Distinct from a bounce, which is <see cref="IChequeService"/>'s job.</summary>
    Task CancelAsync(Guid id, CancellationToken cancellationToken);

    Task<PaymentSummaryDto> GetSummaryAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<DuesSummaryDto> GetDuesAsync(CancellationToken cancellationToken);

    /// <summary>A manual correction — the only way to move a balance with no document behind it.</summary>
    Task AdjustAsync(AdjustPartyBalanceRequest request, CancellationToken cancellationToken);
}
