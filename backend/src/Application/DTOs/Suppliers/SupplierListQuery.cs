namespace Application.DTOs.Suppliers;

public record SupplierListQuery(string? Search, bool? ActiveOnly, int Page = 1, int PageSize = 20);
