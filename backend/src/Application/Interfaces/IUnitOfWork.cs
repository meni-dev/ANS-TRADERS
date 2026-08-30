namespace Application.Interfaces;

/// <summary>
/// Runs a piece of work inside one database transaction.
/// <para>
/// Needed because a document number is claimed before the document is written. Without a
/// transaction spanning both, a bill that fails to save still consumes its number and leaves a hole
/// in the series — and a gap in a bill series is the first thing an auditor asks about. Wrapping
/// them together means a number is taken only by a document that survives.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken);
}
