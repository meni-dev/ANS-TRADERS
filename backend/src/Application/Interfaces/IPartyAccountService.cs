using Application.DTOs.Payments;

namespace Application.Interfaces;

/// <summary>
/// The read side of a party's account: what they owe, what it is made of, and how it got there.
/// <para>
/// Separate from <see cref="IPaymentService"/> because nothing here writes. These are the three
/// questions asked at the counter — "what is his balance", "which bills are open", "should I give
/// him credit" — and they are asked from the customer screen, the billing screen and the statement,
/// none of which are recording a payment when they ask.
/// </para>
/// </summary>
public interface IPartyAccountService
{
    /// <summary>A dated statement, oldest first, with the balance carried into the range.</summary>
    Task<PartyStatementDto> GetStatementAsync(
        Guid? customerId,
        Guid? supplierId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Open documents oldest first — what the allocation picker offers, in FIFO order.</summary>
    Task<IReadOnlyList<OpenDocumentDto>> GetOpenDocumentsAsync(
        Guid? customerId, Guid? supplierId, CancellationToken cancellationToken);

    /// <summary>Everything the billing screen warns on. Never blocks a sale — see the credit rules.</summary>
    Task<CustomerAccountSummaryDto> GetCustomerAccountSummaryAsync(
        Guid customerId, CancellationToken cancellationToken);
}
