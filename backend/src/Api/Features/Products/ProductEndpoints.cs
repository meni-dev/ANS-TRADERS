using Application.DTOs.Products;
using Application.Interfaces;

namespace Api.Features.Products;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", async (
            string? search,
            bool? activeOnly,
            int page,
            int pageSize,
            IProductService service,
            CancellationToken cancellationToken) =>
        {
            var query = new ProductListQuery(
                search, activeOnly, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);

            var result = await service.SearchAsync(query, cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(product);
        });

        group.MapPost("/", async (
            CreateProductRequest request, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/products/{product.Id}", product);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateProductRequest request, IProductService service, CancellationToken cancellationToken) =>
        {
            var product = await service.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(product);
        });

        group.MapDelete("/{id:guid}", async (Guid id, IProductService service, CancellationToken cancellationToken) =>
        {
            await service.DeactivateAsync(id, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/activate", async (Guid id, IProductService service, CancellationToken cancellationToken) =>
        {
            await service.ActivateAsync(id, cancellationToken);
            return Results.NoContent();
        });

        // Two calls on purpose: the first says what the file would do and writes nothing, the
        // second does it. See IProductImportService.
        group.MapPost("/import/preview", async (
            ProductImportRequest request,
            IProductImportService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.PreviewAsync(request, cancellationToken)));

        group.MapPost("/import", async (
            ProductImportRequest request,
            IProductImportService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ImportAsync(request, cancellationToken)));
    }
}
