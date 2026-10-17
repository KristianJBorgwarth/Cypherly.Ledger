using System.Net;
using FluentAssertions;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.EndpointTest;

public class ReadAccessEndpointTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : IntegrationTestBase(factory)
{
    private readonly Key _creator = LedgerSigner.NewKey();

    private static HttpRequestMessage Request(string path, string? key = null, string? timestamp = null, string? signature = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (key is not null) request.Headers.Add("X-Ledger-Key", key);
        if (timestamp is not null) request.Headers.Add("X-Ledger-Timestamp", timestamp);
        if (signature is not null) request.Headers.Add("X-Ledger-Signature", signature);
        return request;
    }

    private HttpRequestMessage SignedRequest(string path, Guid ledgerId)
    {
        var now = LedgerSigner.Now();
        return Request(
            path,
            Convert.ToBase64String(LedgerSigner.PublicKeyOf(_creator)),
            now.ToString(),
            Convert.ToBase64String(LedgerSigner.SignRead(_creator, ledgerId, now)));
    }

    [Theory]
    [InlineData("events")]
    [InlineData("head")]
    public async Task Get_Given_Valid_Read_Headers_Should_Return_Ok(string resource)
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);

        // Act
        var response = await Client.SendAsync(SignedRequest($"/api/ledger/streams/{ledger.Id}/{resource}", ledger.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("events")]
    [InlineData("head")]
    public async Task Get_Without_Read_Headers_Should_Return_NotFound(string resource)
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);

        // Act
        var response = await Client.SendAsync(Request($"/api/ledger/streams/{ledger.Id}/{resource}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Given_Malformed_Read_Headers_Should_Return_NotFound_Not_BadRequest()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);

        // Act
        var response = await Client.SendAsync(Request($"/api/ledger/streams/{ledger.Id}/events", "not-base64!", "not-a-number", "%%%"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
