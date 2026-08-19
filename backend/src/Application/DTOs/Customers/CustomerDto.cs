namespace Application.DTOs.Customers;

public record CustomerDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? Gstin,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? StateCode,
    string? Pincode,
    decimal CreditLimit,
    int CreditDays,
    decimal OpeningBalance,
    /// <summary>What they owe right now, straight off the party ledger.</summary>
    decimal OutstandingBalance,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
