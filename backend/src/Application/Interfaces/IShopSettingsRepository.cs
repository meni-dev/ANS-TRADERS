using Domain.Entities;

namespace Application.Interfaces;

public interface IShopSettingsRepository
{
    /// <summary>
    /// The one settings row, tracked so callers can edit it. The row is created by migration, so
    /// this never returns null in a migrated database.
    /// </summary>
    Task<ShopSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
