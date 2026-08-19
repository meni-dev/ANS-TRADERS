namespace Application.DTOs.Products;

public record ProductListQuery(string? Search, bool? ActiveOnly, int Page = 1, int PageSize = 20);
