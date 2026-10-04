using FluentAssertions;
using Ledger.Application.Abstractions;
using Ledger.Application.Commands.Append;
using Ledger.Application.Commands.Archive;
using Ledger.Application.Interfaces;
using Ledger.Application.Queries.GetEvents;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.QueryTest.GetEventsTest;

public class GetLedgerEventsQueryHandlerTest : IntegrationTestBase
{
    private readonly GetLedgerEventsQueryHandler _sut;
    private readonly AppendLedgerEventCommandHandler _append;
    private readonly ArchiveLedgerCommandHandler _archive;
    private readonly Key _creator = LedgerSigner.NewKey();

    public GetLedgerEventsQueryHandlerTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var signatureHelper = scope.ServiceProvider.GetRequiredService<ISignatureHelper>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _sut = new GetLedgerEventsQueryHandler(repository);
        _append = new AppendLedgerEventCommandHandler(repository, signatureHelper, unitOfWork);
        _archive = new ArchiveLedgerCommandHandler(repository, signatureHelper, unitOfWork);
    }

    private static GetLedgerEventsQuery Query(Guid ledgerId, int fromVersion = 1, int limit = 500) => new()
    {
        LedgerId = ledgerId,
        FromVersion = fromVersion,
        Limit = limit,
    };

    private async Task<LedgerStream> AppendAsync(LedgerStream ledger, byte[] payload, List<byte[]>? keysAdded = null)
    {
        var command = (LedgerCommands.Append(ledger, payload) with { KeysAdded = keysAdded ?? [] }).SignedBy(_creator);
        var result = await _append.Handle(command, CancellationToken.None);
        result.Success.Should().BeTrue();

        return await LedgerFactory.ReloadAsync(Db, ledger.Id);
    }

    [Fact]
    public async Task Handle_Given_Unknown_Ledger_Should_Return_NotFound()
    {
        // Act
        var result = await _sut.Handle(Query(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_Should_Return_Events_In_Version_Order_With_Head()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        ledger = await AppendAsync(ledger, [4, 5, 6]);
        await AppendAsync(ledger, [7, 8, 9]);

        // Act
        var result = await _sut.Handle(Query(ledger.Id), CancellationToken.None);

        // Assert
        var dto = result.RequiredValue;
        dto.Version.Should().Be(3);
        dto.Archived.Should().BeFalse();
        dto.HasMore.Should().BeFalse();
        dto.Events.Select(e => e.Version).Should().ContainInOrder(1, 2, 3);
        dto.Events.Last().Payload.Should().Equal([7, 8, 9]);
    }

    [Fact]
    public async Task Handle_Should_Return_Fields_Needed_To_Verify_The_Chain()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var newMember = LedgerSigner.PublicKeyOf(LedgerSigner.NewKey());
        await AppendAsync(ledger, [4, 5, 6], keysAdded: [newMember]);

        // Act
        var result = await _sut.Handle(Query(ledger.Id), CancellationToken.None);

        // Assert
        var granted = result.RequiredValue.Events.Single(e => e.Version == 2);
        granted.KeysAdded.Should().ContainSingle().Which.Should().Equal(newMember);
        granted.KeysRemoved.Should().BeEmpty();
        granted.PreviousHash.Should().Equal(ledger.Hash);
        granted.WriteKeyPublic.Should().Equal(LedgerSigner.PublicKeyOf(_creator));
        granted.Signature.Should().Equal(
            LedgerSigner.Sign(_creator, ledger.Id, 2, ledger.Hash, [4, 5, 6], [newMember], []));
    }

    [Fact]
    public async Task Handle_Given_FromVersion_Should_Skip_Earlier_Events()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        ledger = await AppendAsync(ledger, [4, 5, 6]);
        await AppendAsync(ledger, [7, 8, 9]);

        // Act
        var result = await _sut.Handle(Query(ledger.Id, fromVersion: 3), CancellationToken.None);

        // Assert
        result.RequiredValue.Events.Should().ContainSingle().Which.Version.Should().Be(3);
        result.RequiredValue.Version.Should().Be(3);
    }

    [Fact]
    public async Task Handle_Given_FromVersion_Past_Head_Should_Return_Empty_Page()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);

        // Act
        var result = await _sut.Handle(Query(ledger.Id, fromVersion: 99), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.RequiredValue.Events.Should().BeEmpty();
        result.RequiredValue.HasMore.Should().BeFalse();
        result.RequiredValue.Version.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Given_Limit_Should_Page_And_Flag_More()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        ledger = await AppendAsync(ledger, [4, 5, 6]);
        await AppendAsync(ledger, [7, 8, 9]);

        // Act
        var first = await _sut.Handle(Query(ledger.Id, limit: 2), CancellationToken.None);
        var second = await _sut.Handle(Query(ledger.Id, fromVersion: 3, limit: 2), CancellationToken.None);

        // Assert
        first.RequiredValue.Events.Select(e => e.Version).Should().ContainInOrder(1, 2);
        first.RequiredValue.HasMore.Should().BeTrue();
        second.RequiredValue.Events.Should().ContainSingle();
        second.RequiredValue.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Given_Archived_Ledger_Should_Still_Return_Events()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        await _archive.Handle(LedgerCommands.Archive(ledger).SignedBy(_creator), CancellationToken.None);

        // Act
        var result = await _sut.Handle(Query(ledger.Id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.RequiredValue.Archived.Should().BeTrue();
        result.RequiredValue.Events.Should().HaveCount(2);
    }
}
