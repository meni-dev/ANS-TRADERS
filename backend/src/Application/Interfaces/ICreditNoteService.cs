using Application.Common;
using Application.DTOs.Returns;

namespace Application.Interfaces;

/// <summary>
/// Goods coming back from a customer. Raises a credit note against the original bill — never an
/// edit of it, because a document already handed over cannot be rewritten.
/// </summary>
public interface ICreditNoteService
{
    Task<PagedResult<CreditNoteListItemDto>> SearchAsync(
        CreditNoteListQuery query, CancellationToken cancellationToken);

    Task<CreditNoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreditNoteDto> CreateAsync(
        CreateCreditNoteRequest request, CancellationToken cancellationToken);

    /// <summary>Keyed wrong. Everything the note did is put back; the number and the row survive.</summary>
    Task CancelAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>What is still returnable on a bill, line by line. Drives the return screen.</summary>
    Task<ReturnableDocumentDto> GetReturnableAsync(Guid invoiceId, CancellationToken cancellationToken);
}
