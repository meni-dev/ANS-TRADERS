using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Reads for the registers.
/// <para>
/// Everything here is <c>AsNoTracking</c> and read-only. Registers are what the shop hands its
/// accountant, so they are built from the stored documents exactly as issued — nothing is
/// recalculated on the way out.
/// </para>
/// </summary>
public interface IReportRepository
{
    Task<IReadOnlyList<Invoice>> GetInvoicesAsync(
        DateOnly fromDate, DateOnly toDate, bool withItems, CancellationToken cancellationToken);

    Task<IReadOnlyList<Purchase>> GetPurchasesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditNote>> GetCreditNotesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<DebitNote>> GetDebitNotesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<Payment>> GetPaymentsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> GetExpensesAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Parties carrying a balance, either direction. Settled parties are left out.</summary>
    /// <summary>
    /// Every issued bill still carrying a balance, with its customer. The ageing schedule is built
    /// from the bills rather than from the party's running balance, because a balance cannot say
    /// how long any part of it has been sitting there.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetOpenInvoicesAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<Customer> Customers, IReadOnlyList<Supplier> Suppliers)> GetOpenPartiesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Every product with stock or movement. Valuation is an as-of-now figure — stock has one
    /// current level, not a level per date — so this takes no range.
    /// </summary>
    Task<IReadOnlyList<Product>> GetProductsForValuationAsync(CancellationToken cancellationToken);

    /// <summary>
    /// What the shelf held going into <paramref name="fromDate"/>, so a filtered stock register
    /// opens from the truth instead of from zero.
    /// </summary>
    /// <summary>
    /// What each part held at the end of <paramref name="onDate"/>, from the movements themselves.
    /// <para>
    /// <c>Product.StockOnHand</c> only ever answers for today, so a year-end valuation cannot come
    /// from it. Keyed by product id; a part with no movement by that date is simply absent.
    /// </para>
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetStockBalancesOnAsync(
        DateOnly onDate, CancellationToken cancellationToken);

    /// <summary>
    /// What each party's account stood at on <paramref name="onDate"/>, from the party ledger.
    /// Positive means they owe the shop, negative means the shop owes them.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetPartyBalancesOnAsync(
        DateOnly onDate, bool customers, CancellationToken cancellationToken);

    Task<decimal> GetStockBalanceBeforeAsync(
        Guid productId, DateOnly fromDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
