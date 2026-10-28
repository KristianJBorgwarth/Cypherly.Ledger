using Ledger.API.Common;
using PrivacyPass.AspNetCore.Extensions;
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
    private const string LedgerKeyHeader = "X-Ledger-Key";
    private const string LedgerTimestampHeader = "X-Ledger-Timestamp";
    private const string LedgerSignatureHeader = "X-Ledger-Signature";

    public void MapRoutes(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("/api/ledger")
            .WithTags("Ledger")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/streams", async ([FromBody] CreateLedgerRequest req, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateLedgerCommand
            {
                LedgerId = req.LedgerId,
                Payload = req.Payload,
                WriteKey = req.WriteKey,
                Signature = req.Signature,
                InviteKeysAdded = req.InviteKeysAdded
            }, ct);

            return result.Success ? Results.Created($"/api/ledger/streams/{req.LedgerId}", null) : result.ToProblemDetails();
        })
        .RequirePrivateToken()
        .Produces(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

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
                InviteKeysAdded = req.InviteKeysAdded,
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

        group.MapGet("/streams/{id:guid}/events", async (
            [FromRoute] Guid id,
            [FromQuery] int? fromVersion,
            [FromQuery] int? limit,
            [FromHeader(Name = LedgerKeyHeader)] string? key,
            [FromHeader(Name = LedgerTimestampHeader)] string? timestamp,
            [FromHeader(Name = LedgerSignatureHeader)] string? signature,
            ISender sender,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLedgerEventsQuery
            {
                LedgerId = id,
                FromVersion = fromVersion ?? 1,
                Limit = limit ?? LedgerQueryLimits.MaxEventsPerRead,
                WriteKeyPublic = DecodeBase64(key),
                Timestamp = ParseTimestamp(timestamp),
                Signature = DecodeBase64(signature)
            }, ct);

            return result.Success ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .Produces<LedgerEventDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/streams/{id:guid}/head", async (
            [FromRoute] Guid id,
            [FromHeader(Name = LedgerKeyHeader)] string? key,
            [FromHeader(Name = LedgerTimestampHeader)] string? timestamp,
            [FromHeader(Name = LedgerSignatureHeader)] string? signature,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLedgerHeadQuery
            {
                LedgerId = id,
                WriteKeyPublic = DecodeBase64(key),
                Timestamp = ParseTimestamp(timestamp),
                Signature = DecodeBase64(signature)
            }, ct);

            return result.Success ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .Produces<LedgerHeadDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static byte[] DecodeBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return [];

        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) ? buffer[..written] : [];
    }

    private static long ParseTimestamp(string? value) => long.TryParse(value, out var timestamp) ? timestamp : 0;
}
