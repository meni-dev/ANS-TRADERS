namespace Application.DTOs.Suppliers;

public record SupplierDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? Gstin,
    string? ContactPerson,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? StateCode,
    string? Pincode,
    string? PaymentTerms,
    decimal OpeningBalance,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
