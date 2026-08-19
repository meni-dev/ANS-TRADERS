using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ShopSettingsRepository : IShopSettingsRepository
{
    private readonly AppDbContext _context;

    public ShopSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ShopSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.ShopSettings
            .FirstOrDefaultAsync(s => s.Id == ShopSettings.SingletonId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        // The migration seeds the row, so this only fires against a database built some other way.
        // Creating defaults beats throwing: a shop with an unconfigured address can still bill.
        settings = new ShopSettings { Id = ShopSettings.SingletonId };
        await _context.ShopSettings.AddAsync(settings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);
}
