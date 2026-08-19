using Application.Common;
using Application.DTOs.Returns;

namespace Application.Interfaces;

/// <summary>
/// Goods coming back from a customer. Raises a credit note against the original bill — never an
/// edit of it, because a document already handed over cannot be rewritten.
/// </summary>
public interface IDebitNoteService
{
    Task<PagedResult<DebitNoteListItemDto>> SearchAsync(
        DebitNoteListQuery query, CancellationToken cancellationToken);

    Task<DebitNoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DebitNoteDto> CreateAsync(
        CreateDebitNoteRequest request, CancellationToken cancellationToken);

    /// <summary>Keyed wrong. Everything the note did is put back; the number and the row survive.</summary>
    Task CancelAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>What is still returnable on a bill, line by line. Drives the return screen.</summary>
    Task<ReturnableDocumentDto> GetReturnableAsync(Guid purchaseId, CancellationToken cancellationToken);
}
