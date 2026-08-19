namespace Application.DTOs.Customers;

public record CreateCustomerRequest(
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
    /// <summary>Days before a bill falls due. Zero means payment on delivery.</summary>
    int CreditDays,
    decimal OpeningBalance);
