namespace Application.DTOs.Customers;

/// <summary>
/// Opening balance is absent by design: it describes the state at the moment the customer was
/// created and is corrected through ledger entries, never by editing the master record.
/// </summary>
public record UpdateCustomerRequest(
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
    bool IsActive);
