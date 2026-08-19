namespace Application.DTOs.Customers;

public record CustomerListQuery(string? Search, bool? ActiveOnly, int Page = 1, int PageSize = 20);
