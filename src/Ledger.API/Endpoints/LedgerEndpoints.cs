using Ledger.API.Common;
using Ledger.API.Requests;
using Ledger.Application.Commands.Append;
using Ledger.Application.Commands.Archive;
using Ledger.Application.Commands.Create;
using Ledger.Application.Common;
using Ledger.Application.Queries.GetEvents;
using Ledger.Application.Queries.GetHead;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Ledger.Application.Dto;

internal sealed class LedgerEndpoints : IEndpoint
{
    public void MapRoutes(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/ledger")
            .WithTags("Ledger")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/streams", async ([FromBody] CreateLedgerRequest req, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateLedgerCommand
            {
                LedgerId = req.LedgerId,
                Payload = req.Payload,
                WriteKey = req.WriteKey,
                Signature = req.Signature,
                KeysAdded = req.KeysAdded
            }, ct);

            return result.Success ? Results.Created($"/api/ledger/streams/{req.LedgerId}", null) : result.ToProblemDetails();
        })
        .Produces(StatusCodes.Status201Created);

        group.MapPut("/streams/{id:guid}/events", async ([FromRoute] Guid id, [FromBody] AppendLedgerEventRequest req, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new AppendLedgerEventCommand
            {
                LedgerId = id,
                ExpectedVersion = req.ExpectedVersion,
                PreviousHash = req.PreviousHash,
                Payload = req.Payload,
                WriteKeyPublic = req.WriteKeyPublic,
                Signature = req.Signature,
                KeysAdded = req.KeysAdded,
                KeysRemoved = req.KeysRemoved
            }, ct);

            return result.Success ? Results.Ok() : result.ToProblemDetails();
        })
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/streams/{id:guid}/archive", async ([FromRoute] Guid id, [FromBody] ArchiveLedgerRequest req, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new ArchiveLedgerCommand
            {
                LedgerId = id,
                ExpectedVersion = req.ExpectedVersion,
                PreviousHash = req.PreviousHash,
                Payload = req.Payload,
                WriteKeyPublic = req.WriteKeyPublic,
                Signature = req.Signature,
            }, ct);

            return result.Success ? Results.Ok() : result.ToProblemDetails();
        })
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/streams/{id:guid}/events", async ([FromRoute] Guid id, [FromQuery] int? fromVersion, [FromQuery] int? limit, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLedgerEventsQuery
            {
                LedgerId = id,
                FromVersion = fromVersion ?? 1,
                Limit = limit ?? LedgerQueryLimits.MaxEventsPerRead
            }, ct);

            return result.Success ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .Produces<LedgerEventDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/streams/{id:guid}/head", async ([FromRoute] Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLedgerHeadQuery { LedgerId = id }, ct);

            return result.Success ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .Produces<LedgerHeadDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
