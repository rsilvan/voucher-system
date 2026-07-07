using VoucherSystem.Contracts;

namespace VoucherSystem.Api.Endpoints;

public static class SelfServiceEndpoints
{
    public static void MapSelfServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/self-service");

        group.MapPost("/organizations", async (CreateOrganizationRequest request,
            Application.IOrganizationService service,
            HttpContext httpContext,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SelfService");
            try
            {
                var result = await service.CreateOrganizationAsync(request);
                return Results.Created($"/api/organizations/{result.OrganizationId}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ErrorResponse { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Failed to create organization");
                return Results.StatusCode(500);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error creating organization");
                return Results.Json(new ErrorResponse { Error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}
