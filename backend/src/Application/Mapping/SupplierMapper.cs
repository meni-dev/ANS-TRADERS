using Application.DTOs.Suppliers;
using Domain.Entities;

namespace Application.Mapping;

public static class SupplierMapper
{
    public static SupplierDto ToDto(this Supplier supplier) => new(
        supplier.Id,
        supplier.Name,
        supplier.Phone,
        supplier.Email,
        supplier.Gstin,
        supplier.ContactPerson,
        supplier.AddressLine1,
        supplier.AddressLine2,
        supplier.City,
        supplier.State,
        supplier.StateCode,
        supplier.Pincode,
        supplier.PaymentTerms,
        supplier.OpeningBalance,
        supplier.IsActive,
        supplier.CreatedAt,
        supplier.UpdatedAt);
}
