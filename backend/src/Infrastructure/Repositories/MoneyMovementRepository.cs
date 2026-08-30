using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MoneyMovementRepository : IMoneyMovementRepository
{
    private readonly AppDbContext _context;

    public MoneyMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MoneyMovement>> SearchAsync(
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
        await _context.MoneyMovements
            .AsNoTracking()
            .Where(m => m.MovementDate >= fromDate && m.MovementDate <= toDate)
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<MoneyMovement?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.MoneyMovements.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<MoneyMovementKind, decimal>> GetTotalsAsync(
        CancellationToken cancellationToken) =>
        await _context.MoneyMovements
            .AsNoTracking()
            .Where(m => !m.IsCancelled)
            .GroupBy(m => m.Kind)
            .Select(g => new { Kind = g.Key, Total = g.Sum(m => m.Amount) })
            .ToDictionaryAsync(x => x.Kind, x => x.Total, cancellationToken);

    public Task<bool> ExistsAsync(MoneyMovementKind kind, CancellationToken cancellationToken) =>
        _context.MoneyMovements.AnyAsync(m => m.Kind == kind && !m.IsCancelled, cancellationToken);

    public async Task AddAsync(MoneyMovement movement, CancellationToken cancellationToken) =>
        await _context.MoneyMovements.AddAsync(movement, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
