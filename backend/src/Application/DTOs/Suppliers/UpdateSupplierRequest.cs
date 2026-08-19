namespace Application.DTOs.Suppliers;

/// <summary>
/// Opening balance is absent by design: it describes the state at the moment the supplier was
/// created and is corrected through ledger entries, never by editing the master record.
/// </summary>
public record UpdateSupplierRequest(
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
    bool IsActive);
