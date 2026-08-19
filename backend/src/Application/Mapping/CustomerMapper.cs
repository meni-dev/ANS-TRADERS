using Application.DTOs.Customers;
using Domain.Entities;

namespace Application.Mapping;

public static class CustomerMapper
{
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Phone,
        customer.Email,
        customer.Gstin,
        customer.AddressLine1,
        customer.AddressLine2,
        customer.City,
        customer.State,
        customer.StateCode,
        customer.Pincode,
        customer.CreditLimit,
        customer.CreditDays,
        customer.OpeningBalance,
        customer.OutstandingBalance,
        customer.IsActive,
        customer.CreatedAt,
        customer.UpdatedAt);
}
