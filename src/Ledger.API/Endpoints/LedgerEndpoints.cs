using Ledger.API.Common;

internal sealed class LedgerEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/streams")
            .WithTags("Ledger")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
