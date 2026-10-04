using FluentAssertions;
using Ledger.Application.Abstractions;
using Ledger.Application.Commands.Append;
using Ledger.Application.Interfaces;
using Ledger.Domain.Common;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.CommandTest.AppendTest;

public class AppendLedgerEventCommandHandlerTest : IntegrationTestBase
{
    private readonly AppendLedgerEventCommandHandler _sut;
    private readonly Key _creator = LedgerSigner.NewKey();

    public AppendLedgerEventCommandHandlerTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var signatureHelper = scope.ServiceProvider.GetRequiredService<ISignatureHelper>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _sut = new AppendLedgerEventCommandHandler(repository, signatureHelper, unitOfWork);
    }

    [Fact]
    public async Task Handle_Given_Valid_Command_Should_Append_And_Chain_To_Previous_Event()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Append(ledger).SignedBy(_creator);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var stored = await Db.LedgerStream.AsNoTracking().Include(l => l.Events).FirstAsync(l => l.Id == ledger.Id);

        stored.Version.Should().Be(2);
        stored.Hash.Should().Equal(LedgerSigner.Hash(ledger.Id, 2, ledger.Hash, command.Payload));

        var appended = stored.Events.Single(e => e.Version == 2);
        appended.PreviousHash.Should().Equal(ledger.Hash);
        appended.Payload.Should().Equal(command.Payload);
    }

    [Fact]
    public async Task Handle_Given_Unknown_Ledger_Should_Return_NotFound()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Append(ledger).SignedBy(_creator) with { LedgerId = Guid.NewGuid() };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_Given_Signature_From_Unknown_Key_Should_Return_Forbidden()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Append(ledger).SignedBy(LedgerSigner.NewKey());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        Db.LedgerEvent.AsNoTracking().Where(e => e.StreamId == ledger.Id).Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Given_Payload_Changed_After_Signing_Should_Return_Forbidden()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Append(ledger).SignedBy(_creator) with { Payload = [9, 9, 9] };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_Given_Signature_Over_Expected_Version_Should_Return_Forbidden()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Append(ledger) with
        {
            WriteKeyPublic = LedgerSigner.PublicKeyOf(_creator),
            Signature = LedgerSigner.Sign(_creator, ledger.Id, ledger.Version, ledger.Hash, [4, 5, 6]),
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_Given_Stale_Expected_Version_Should_Return_Conflict()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = (LedgerCommands.Append(ledger) with
        {
            ExpectedVersion = 0,
            PreviousHash = LedgerFactory.GenesisHash,
        }).SignedBy(_creator);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        Db.LedgerEvent.AsNoTracking().Where(e => e.StreamId == ledger.Id).Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Given_Key_Added_Should_Let_That_Key_Append_Next()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var newMember = LedgerSigner.NewKey();

        var grantCommand = (LedgerCommands.Append(ledger) with
        {
            KeysAdded = [LedgerSigner.PublicKeyOf(newMember)],
        }).SignedBy(_creator);

        // Act
        var grant = await _sut.Handle(grantCommand, CancellationToken.None);

        var afterGrant = await LedgerFactory.ReloadAsync(Db, ledger.Id);
        var second = await _sut.Handle(LedgerCommands.Append(afterGrant, [7, 7, 7]).SignedBy(newMember), CancellationToken.None);

        // Assert
        grant.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        afterGrant.WriteKeys.Should().HaveCount(2);
        Db.LedgerEvent.AsNoTracking().Where(e => e.StreamId == ledger.Id).Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Given_Key_Removed_Should_Reject_That_Key_Afterwards()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var newMember = LedgerSigner.NewKey();

        await _sut.Handle((LedgerCommands.Append(ledger) with
        {
            KeysAdded = [LedgerSigner.PublicKeyOf(newMember)],
        }).SignedBy(_creator), CancellationToken.None);

        var afterGrant = await LedgerFactory.ReloadAsync(Db, ledger.Id);

        // Act
        await _sut.Handle((LedgerCommands.Append(afterGrant, [8, 8, 8]) with
        {
            KeysRemoved = [LedgerSigner.PublicKeyOf(newMember)],
        }).SignedBy(_creator), CancellationToken.None);

        var afterRevoke = await LedgerFactory.ReloadAsync(Db, ledger.Id);
        var result = await _sut.Handle(LedgerCommands.Append(afterRevoke, [9, 9, 9]).SignedBy(newMember), CancellationToken.None);

        // Assert
        afterRevoke.WriteKeys.Should().ContainSingle();
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
