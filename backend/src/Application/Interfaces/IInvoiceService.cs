using Application.Common;
using Application.DTOs.Invoices;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> SearchAsync(
        InvoiceListQuery query, CancellationToken cancellationToken);

    Task<InvoiceDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken);

    /// <summary>Voids the document. Issued invoices are never deleted — see <c>InvoiceStatus</c>.</summary>
    Task CancelAsync(Guid id, CancellationToken cancellationToken);
}
