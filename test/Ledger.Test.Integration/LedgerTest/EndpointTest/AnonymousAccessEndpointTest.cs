using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.EndpointTest;

public class AnonymousAccessEndpointTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : IntegrationTestBase(factory)
{
    private readonly Key _creator = LedgerSigner.NewKey();

    private static HttpRequestMessage Request(HttpMethod method, string path, object body, bool authenticated)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        if (authenticated) request.Headers.Add("Authorization", "Bearer test");
        return request;
    }

    private object CreateBody()
    {
        var cmd = LedgerCommands.Create(Guid.NewGuid()).SignedBy(_creator);
        return new { cmd.LedgerId, cmd.Payload, cmd.WriteKey, cmd.Signature, cmd.InviteKeysAdded };
    }

    [Fact]
    public async Task Create_Without_Authorization_Should_Return_Unauthorized()
    {
        // Act
        var response = await Client.SendAsync(Request(HttpMethod.Post, "/api/ledger/streams", CreateBody(), authenticated: false));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_With_Jwt_Should_Return_Unauthorized()
    {
        // Act
        var response = await Client.SendAsync(Request(HttpMethod.Post, "/api/ledger/streams", CreateBody(), authenticated: true));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Append_Without_Authorization_Given_Valid_Signature_Should_Return_Ok()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var cmd = LedgerCommands.Append(ledger).SignedBy(_creator);
        var body = new { cmd.ExpectedVersion, cmd.PreviousHash, cmd.Payload, cmd.WriteKeyPublic, cmd.Signature, cmd.KeysAdded, cmd.InviteKeysAdded, cmd.KeysRemoved };

        // Act
        var response = await Client.SendAsync(Request(HttpMethod.Put, $"/api/ledger/streams/{ledger.Id}/events", body, authenticated: false));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Append_Without_Authorization_Given_Unknown_Write_Key_Should_Return_Forbidden()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var cmd = LedgerCommands.Append(ledger).SignedBy(LedgerSigner.NewKey());
        var body = new { cmd.ExpectedVersion, cmd.PreviousHash, cmd.Payload, cmd.WriteKeyPublic, cmd.Signature, cmd.KeysAdded, cmd.InviteKeysAdded, cmd.KeysRemoved };

        // Act
        var response = await Client.SendAsync(Request(HttpMethod.Put, $"/api/ledger/streams/{ledger.Id}/events", body, authenticated: false));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Archive_Without_Authorization_Given_Valid_Signature_Should_Return_Ok()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var cmd = LedgerCommands.Archive(ledger).SignedBy(_creator);
        var body = new { cmd.ExpectedVersion, cmd.PreviousHash, cmd.Payload, cmd.WriteKeyPublic, cmd.Signature };

        // Act
        var response = await Client.SendAsync(Request(HttpMethod.Put, $"/api/ledger/streams/{ledger.Id}/archive", body, authenticated: false));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
