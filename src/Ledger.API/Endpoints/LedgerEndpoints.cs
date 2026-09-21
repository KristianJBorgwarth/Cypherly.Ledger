using Ledger.API.Common;
using Ledger.API.Requests;
using Ledger.Application.Commands.Create;
using Mediator;
using Microsoft.AspNetCore.Mvc;

internal sealed class LedgerEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/ledger")
            .WithTags("Ledger")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/streams", async ([FromBody] CreateLedgerRequest req, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateLedgerCommand
            {
                LedgerId = req.LedgerId,
                Payload = req.Payload,
                WriteKey = req.WriteKey,
                Signature = req.Signature
            }, ct);

            return result.Success ? Results.Created($"/api/ledger/streams/{req.LedgerId}", null) : result.ToProblemDetails();
        })
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

    }
}
