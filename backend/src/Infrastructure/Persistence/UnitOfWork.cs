using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <inheritdoc />
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        // Already inside one — a caller wrapping a caller. Joining it rather than starting a second
        // keeps the outer boundary meaningful; nesting would let an inner commit make an outer
        // rollback impossible.
        if (_context.Database.CurrentTransaction is not null)
        {
            return await work();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var result = await work();

        await transaction.CommitAsync(cancellationToken);

        // No catch: an exception leaves the transaction uncommitted, and disposing it rolls the
        // whole thing back — the number included. Swallowing anything here would be the one way to
        // commit half a document.
        return result;
    }
}
