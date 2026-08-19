using Application.Common;
using Application.DTOs.Payments;

namespace Application.Interfaces;

/// <summary>
/// Walks a cheque from the drawer to the bank and back. Owns the transitions, calls
/// <see cref="IPaymentLedger"/> when money has to move, and does the one save.
/// </summary>
public interface IChequeService
{
    Task<PagedResult<PaymentListItemDto>> SearchAsync(
        ChequeListQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Handed to the bank. Nothing moves — a current-dated cheque already settled its bills when it
    /// was taken, and a post-dated one settles them on <see cref="PostAsync"/>.
    /// </summary>
    Task<PaymentDto> DepositAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken);

    /// <summary>
    /// The bank paid. A post-dated cheque posts here if it has not already, so the money lands in
    /// the month it actually arrived.
    /// </summary>
    Task<PaymentDto> ClearAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken);

    /// <summary>
    /// The bank refused. Every allocation is released, the bills go back to unpaid, and the party's
    /// balance climbs again — recorded as a bounce rather than a cancellation, because it genuinely
    /// happened and the shop needs to be able to see that it did.
    /// </summary>
    Task<PaymentDto> BounceAsync(
        Guid paymentId, BounceChequeRequest request, CancellationToken cancellationToken);

    /// <summary>Handed back before banking — swapped for cash, or the customer wanted it returned.</summary>
    Task<PaymentDto> CancelAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken);

    /// <summary>
    /// A post-dated cheque taken to the bank: it settles its documents now, dated the day it was
    /// banked rather than the day it was handed over.
    /// </summary>
    Task<PaymentDto> PostAsync(Guid paymentId, DateOnly onDate, CancellationToken cancellationToken);
}
