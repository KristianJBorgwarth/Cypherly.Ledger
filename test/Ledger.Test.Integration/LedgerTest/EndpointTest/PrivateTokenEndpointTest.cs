using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using FluentAssertions;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.EntityFrameworkCore;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.EndpointTest;

public class PrivateTokenEndpointTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : IntegrationTestBase(factory)
{
    private readonly Key _creator = LedgerSigner.NewKey();

    private Task<HttpResponseMessage> CreateAsync(string? authorization)
    {
        var cmd = LedgerCommands.Create(Guid.NewGuid()).SignedBy(_creator);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ledger/streams")
        {
            Content = JsonContent.Create(new { cmd.LedgerId, cmd.Payload, cmd.WriteKey, cmd.Signature, cmd.KeysAdded }),
        };

        if (authorization is not null) request.Headers.TryAddWithoutValidation("Authorization", authorization);
        return Client.SendAsync(request);
    }

    [Fact]
    public async Task Create_Given_Private_Token_Should_Return_Created()
    {
        // Act
        var response = await CreateAsync(PrivateTokens.Header());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Given_Private_Token_Should_Spend_It()
    {
        // Arrange
        var before = await Db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM spent_token").SingleAsync();

        // Act
        await CreateAsync(PrivateTokens.Header());

        // Assert
        var after = await Db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM spent_token").SingleAsync();
        after.Should().Be(before + 1);
    }

    [Fact]
    public async Task Create_Given_Same_Private_Token_Twice_Should_Reject_The_Second()
    {
        // Arrange
        var header = PrivateTokens.Header();

        // Act
        var first = await CreateAsync(header);
        var second = await CreateAsync(header);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Given_Token_For_Other_Origin_Should_Return_Unauthorized()
    {
        // Act
        var response = await CreateAsync(PrivateTokens.Header(origin: "keystore"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Given_Token_From_Other_Issuer_Should_Return_Unauthorized()
    {
        // Arrange
        using var otherIssuer = RSA.Create(2048);

        // Act
        var response = await CreateAsync(PrivateTokens.Header(issuer: otherIssuer));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Given_Jwt_Should_Return_Unauthorized()
    {
        // Act
        var response = await CreateAsync("Bearer test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
