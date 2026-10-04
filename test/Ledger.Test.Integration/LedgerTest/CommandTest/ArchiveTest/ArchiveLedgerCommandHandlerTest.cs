using FluentAssertions;
using Ledger.Application.Abstractions;
using Ledger.Application.Commands.Append;
using Ledger.Application.Commands.Archive;
using Ledger.Application.Interfaces;
using Ledger.Domain.Common;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.CommandTest.ArchiveTest;

public class ArchiveLedgerCommandHandlerTest : IntegrationTestBase
{
    private readonly ArchiveLedgerCommandHandler _sut;
    private readonly AppendLedgerEventCommandHandler _append;
    private readonly Key _creator = LedgerSigner.NewKey();

    public ArchiveLedgerCommandHandlerTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var signatureHelper = scope.ServiceProvider.GetRequiredService<ISignatureHelper>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _sut = new ArchiveLedgerCommandHandler(repository, signatureHelper, unitOfWork);
        _append = new AppendLedgerEventCommandHandler(repository, signatureHelper, unitOfWork);
    }

    [Fact]
    public async Task Handle_Given_Valid_Command_Should_Archive_Ledger()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Archive(ledger).SignedBy(_creator);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var stored = await Db.LedgerStream.AsNoTracking().Include(l => l.Events).FirstAsync(l => l.Id == ledger.Id);
        stored.Archived.Should().BeTrue();
        stored.Version.Should().Be(2);

        var archiveEvent = stored.Events.Single(e => e.Version == 2);
        archiveEvent.PreviousHash.Should().Equal(ledger.Hash);
        archiveEvent.Payload.Should().Equal(command.Payload);
    }

    [Fact]
    public async Task Handle_Given_Unknown_Ledger_Should_Return_NotFound()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var command = LedgerCommands.Archive(ledger).SignedBy(_creator) with { LedgerId = Guid.NewGuid() };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_Given_Key_Without_Signature_Should_Return_Forbidden()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var impostor = LedgerSigner.NewKey();

        var command = LedgerCommands.Archive(ledger).SignedBy(impostor) with
        {
            WriteKeyPublic = LedgerSigner.PublicKeyOf(_creator),
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        (await LedgerFactory.ReloadAsync(Db, ledger.Id)).Archived.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Given_Key_Not_On_Ledger_Should_Return_Forbidden()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var stranger = LedgerSigner.NewKey();
        var command = LedgerCommands.Archive(ledger).SignedBy(stranger);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        (await LedgerFactory.ReloadAsync(Db, ledger.Id)).Archived.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Given_Stale_Head_Should_Return_Conflict()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        var staleSignature = LedgerCommands.Archive(ledger).SignedBy(_creator);

        await _append.Handle(LedgerCommands.Append(ledger).SignedBy(_creator), CancellationToken.None);

        // Act
        var result = await _sut.Handle(staleSignature, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        (await LedgerFactory.ReloadAsync(Db, ledger.Id)).Archived.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Given_Already_Archived_Should_Return_NotFound()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        await _sut.Handle(LedgerCommands.Archive(ledger).SignedBy(_creator), CancellationToken.None);

        var archived = await LedgerFactory.ReloadAsync(Db, ledger.Id);

        // Act
        var result = await _sut.Handle(LedgerCommands.Archive(archived).SignedBy(_creator), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_Given_Archived_Ledger_Should_Reject_Further_Appends()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        await _sut.Handle(LedgerCommands.Archive(ledger).SignedBy(_creator), CancellationToken.None);

        var archived = await LedgerFactory.ReloadAsync(Db, ledger.Id);

        // Act
        var result = await _append.Handle(LedgerCommands.Append(archived).SignedBy(_creator), CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        Db.LedgerEvent.AsNoTracking().Where(e => e.StreamId == ledger.Id).Should().HaveCount(2);
    }
}
