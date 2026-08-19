using Application.DTOs.Settings;

namespace Application.Interfaces;

public interface IShopSettingsService
{
    Task<ShopSettingsDto> GetAsync(CancellationToken cancellationToken);

    Task<ShopSettingsDto> UpdateAsync(
        UpdateShopSettingsRequest request, CancellationToken cancellationToken);

    Task<ShopSettingsDto> SetBooksLockAsync(
        SetBooksLockRequest request, CancellationToken cancellationToken);
}
