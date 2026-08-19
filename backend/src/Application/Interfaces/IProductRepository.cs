using Domain.Entities;

namespace Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        string? search, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken);

    Task<bool> PartNumberExistsAsync(string partNumber, Guid? excludeId, CancellationToken cancellationToken);

    /// <summary>
    /// Every product whose part number appears in the given set, keyed by part number.
    /// <para>
    /// One query rather than a existence check per row: a catalogue file is thousands of rows, and
    /// asking the database once per row turns a five-second import into a five-minute one.
    /// </para>
    /// </summary>
    Task<IReadOnlyDictionary<string, Product>> GetByPartNumbersAsync(
        IReadOnlyCollection<string> partNumbers, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
