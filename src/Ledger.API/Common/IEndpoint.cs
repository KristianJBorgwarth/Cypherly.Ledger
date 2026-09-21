namespace Ledger.API.Common;

internal interface IEndpoint
{
    void MapRoutes(IEndpointRouteBuilder routeBuilder);
}
