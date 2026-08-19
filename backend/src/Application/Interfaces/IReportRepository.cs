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
    Task<(IReadOnlyList<Customer> Customers, IReadOnlyList<Supplier> Suppliers)> GetOpenPartiesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Every product with stock or movement. Valuation is an as-of-now figure — stock has one
    /// current level, not a level per date — so this takes no range.
    /// </summary>
    Task<IReadOnlyList<Product>> GetProductsForValuationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}
