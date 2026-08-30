namespace Application.DTOs.Settings;

public record ShopSettingsDto(
    string Name,
    string? LegalName,
    string? Gstin,
    string StateCode,
    string State,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Pincode,
    string? Phone,
    string? Email,
    string? InvoiceFooter,
    string? BankDetails,
    string? InvoiceTerms,
    string InvoiceTemplate,
    DateOnly? BooksLockedUpTo,
    /// <summary>The day the shop's books begin. Nothing may be dated before it.</summary>
    DateOnly? BooksStartFrom);

/// <summary>
/// Null clears the lock. Deliberately a separate request from the rest of settings: moving the lock
/// is an owner-only act that gets logged, and folding it into the address form would let it move by
/// accident every time somebody fixed a phone number.
/// </summary>
public record SetBooksLockRequest(DateOnly? LockedUpTo);

public record UpdateShopSettingsRequest(
    /// <summary>Null leaves the books open at the near end — see <see cref="ShopSettingsDto"/>.</summary>
    DateOnly? BooksStartFrom,
    string Name,
    string? LegalName,
    string? Gstin,
    string StateCode,
    string State,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Pincode,
    string? Phone,
    string? Email,
    string? InvoiceFooter,
    string? BankDetails,
    string? InvoiceTerms,
    string InvoiceTemplate);
