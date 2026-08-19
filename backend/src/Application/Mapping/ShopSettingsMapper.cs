using Application.DTOs.Settings;
using Domain.Entities;

namespace Application.Mapping;

public static class ShopSettingsMapper
{
    public static ShopSettingsDto ToDto(this ShopSettings settings) => new(
        settings.Name,
        settings.LegalName,
        settings.Gstin,
        settings.StateCode,
        settings.State,
        settings.AddressLine1,
        settings.AddressLine2,
        settings.City,
        settings.Pincode,
        settings.Phone,
        settings.Email,
        settings.InvoiceFooter,
        settings.BankDetails,
        settings.InvoiceTerms,
        settings.InvoiceTemplate.ToString(),
        settings.BooksLockedUpTo);
}
