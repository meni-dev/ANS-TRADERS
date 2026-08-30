using Application.Common;

namespace Api.Features.Business;

public static class BusinessEndpoints
{
    /// <summary>
    /// Exposes the shop's own details so the printed invoice can render its seller header from the
    /// same configuration the tax split is decided by, instead of hard-coding it in the frontend.
    /// </summary>
    public static void MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/business-profile", (BusinessProfile profile) => Results.Ok(profile))
            .WithTags("Business");
    }
}
