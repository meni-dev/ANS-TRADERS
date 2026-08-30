using Application.Common;
using Application.Common.Exceptions;
using Application.DTOs.Settings;
using Application.Interfaces;
using Application.Mapping;
using Domain.Enums;
using FluentValidation;

namespace Application.Services;

public class ShopSettingsService : IShopSettingsService
{
    private readonly IShopSettingsRepository _repository;
    private readonly IValidator<UpdateShopSettingsRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    private readonly IShopClock _clock;

    public ShopSettingsService(
        IShopSettingsRepository repository,
        IValidator<UpdateShopSettingsRequest> updateValidator,
        ICurrentUser currentUser,
        IAuditLog audit,
        IShopClock clock)
    {
        _repository = repository;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
        _audit = audit;
        _clock = clock;
    }

    public async Task<ShopSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        return settings.ToDto();
    }

    public async Task<ShopSettingsDto> UpdateAsync(
        UpdateShopSettingsRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.SettingsEdit, "change the shop's settings");

        await ValidationHelper.ValidateAsync(_updateValidator, request, cancellationToken);

        // Parsed before anything is assigned, so an unknown template name cannot leave the row
        // half-updated with a valid address and an invalid layout.
        if (!Enum.TryParse<InvoiceTemplate>(request.InvoiceTemplate, ignoreCase: true, out var template))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["InvoiceTemplate"] = [$"'{request.InvoiceTemplate}' is not a template this app knows"],
            });
        }

        var settings = await _repository.GetAsync(cancellationToken);

        // Captured before the row is written over, so the log can say what it changed from.
        var previousGstin = settings.Gstin;
        var previousStateCode = settings.StateCode;
        var previousName = settings.Name;
        var previousTemplate = settings.InvoiceTemplate.ToString();

        settings.Name = request.Name.Trim();
        settings.LegalName = Clean(request.LegalName);
        settings.Gstin = Clean(request.Gstin)?.ToUpperInvariant();
        settings.StateCode = request.StateCode.Trim();
        settings.State = request.State.Trim();
        settings.AddressLine1 = Clean(request.AddressLine1);
        settings.AddressLine2 = Clean(request.AddressLine2);
        settings.City = Clean(request.City);
        settings.Pincode = Clean(request.Pincode);
        settings.Phone = Clean(request.Phone);
        settings.Email = Clean(request.Email);
        settings.InvoiceFooter = Clean(request.InvoiceFooter);
        settings.BankDetails = Clean(request.BankDetails);
        settings.InvoiceTerms = Clean(request.InvoiceTerms);
        settings.InvoiceTemplate = template;
        settings.BooksStartFrom = request.BooksStartFrom;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        // Only the fields that change what a bill legally says. An address correction is not worth a
        // log line; the GSTIN and the state are, because every invoice carries them and the return
        // is filed under them — and a change to either was previously invisible.
        var changes = new List<string>();

        void Note(string field, string? before, string? after)
        {
            if (!string.Equals(before ?? string.Empty, after ?? string.Empty, StringComparison.Ordinal))
            {
                changes.Add($"{field} {(string.IsNullOrWhiteSpace(before) ? "unset" : before)} to "
                    + $"{(string.IsNullOrWhiteSpace(after) ? "unset" : after)}");
            }
        }

        Note("GSTIN", previousGstin, settings.Gstin);
        Note("state", previousStateCode, settings.StateCode);
        Note("name", previousName, settings.Name);
        Note("template", previousTemplate, settings.InvoiceTemplate.ToString());

        if (changes.Count > 0)
        {
            await _audit.RecordAsync(
                AuditAction.SettingsChanged, "ShopSettings", entityId: null, entityLabel: settings.Name,
                string.Join("; ", changes), cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return settings.ToDto();
    }

    public async Task<ShopSettingsDto> SetBooksLockAsync(
        SetBooksLockRequest request, CancellationToken cancellationToken)
    {
        _currentUser.Require(Permission.BooksLock, "lock or unlock the books");

        var settings = await _repository.GetAsync(cancellationToken);

        if (request.LockedUpTo is { } lockedUpTo && lockedUpTo > _clock.Today)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                // A lock into the future would stop today's billing, and whoever set it would read
                // the refusal as the app being broken.
                ["LockedUpTo"] = ["The books cannot be locked past today"],
            });
        }

        var previous = settings.BooksLockedUpTo;
        settings.BooksLockedUpTo = request.LockedUpTo;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        // Moving the lock backwards is the interesting one — it is how a filed month becomes
        // editable again — so the log records where it came from as well as where it went.
        await _audit.RecordAsync(
            request.LockedUpTo is null ? AuditAction.BooksUnlocked : AuditAction.BooksLocked,
            "ShopSettings",
            entityId: null,
            entityLabel: null,
            detail: $"{Show(previous)} to {Show(request.LockedUpTo)}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return settings.ToDto();
    }

    private static string Show(DateOnly? date) => date is { } d ? d.ToString("dd-MM-yyyy") : "open";

    /// <summary>
    /// Blank optional fields are stored as null rather than as an empty string, so a template can
    /// test one condition (`is null`) instead of two when deciding whether to print a block.
    /// </summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
