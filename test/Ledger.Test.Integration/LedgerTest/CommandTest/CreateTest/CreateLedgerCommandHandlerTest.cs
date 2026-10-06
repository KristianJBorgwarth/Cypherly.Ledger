using FluentAssertions;
using Ledger.Application.Abstractions;
using Ledger.Application.Commands.Create;
using Ledger.Application.Interfaces;
using Ledger.Domain.Common;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.CommandTest.CreateTest;

public class CreateLedgerCommandHandlerTest : IntegrationTestBase
{
    private readonly CreateLedgerCommandHandler _sut;
    private readonly Key _creator = LedgerSigner.NewKey();

    public CreateLedgerCommandHandlerTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var signatureHelper = scope.ServiceProvider.GetRequiredService<ISignatureHelper>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _sut = new CreateLedgerCommandHandler(repository, signatureHelper, unitOfWork);
    }

    [Fact]
    public async Task Handle_Given_Valid_Command_Should_Persist_Ledger_With_First_Event()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var command = LedgerCommands.Create(ledgerId).SignedBy(_creator);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        var stored = await Db.LedgerStream.AsNoTracking()
            .Include(l => l.WriteKeys)
            .Include(l => l.Events)
            .FirstAsync(l => l.Id == ledgerId);

        stored.Version.Should().Be(LedgerFactory.GenesisVersion);
        stored.Archived.Should().BeFalse();
        stored.Hash.Should().Equal(LedgerSigner.Hash(ledgerId, LedgerFactory.GenesisVersion, LedgerFactory.GenesisHash, command.Payload, command.KeysAdded));
        stored.WriteKeys.Select(k => k.PublicKey).Should().BeEquivalentTo(
            command.KeysAdded.Prepend(LedgerSigner.PublicKeyOf(_creator)));

        var evt = stored.Events.Should().ContainSingle().Subject;
        evt.Version.Should().Be(LedgerFactory.GenesisVersion);
        evt.Payload.Should().Equal(command.Payload);
        evt.PreviousHash.Should().Equal(LedgerFactory.GenesisHash);
        evt.WriteKeyPublic.Should().Equal(LedgerSigner.PublicKeyOf(_creator));
        evt.KeysAdded.Should().BeEquivalentTo(command.KeysAdded);
    }

    [Fact]
    public async Task Handle_Given_Signature_From_Another_Key_Should_Return_Forbidden()
    {
        // Arrange
        var command = LedgerCommands.Create(Guid.NewGuid()).SignedBy(LedgerSigner.NewKey()) with
        {
            WriteKey = LedgerSigner.PublicKeyOf(_creator),
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        Db.LedgerStream.AsNoTracking().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Given_Payload_Changed_After_Signing_Should_Return_Forbidden()
    {
        // Arrange
        var command = LedgerCommands.Create(Guid.NewGuid()).SignedBy(_creator) with { Payload = [9, 9, 9] };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        Db.LedgerStream.AsNoTracking().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Given_Keys_Added_Changed_After_Signing_Should_Return_Forbidden()
    {
        // Arrange
        var command = LedgerCommands.Create(Guid.NewGuid()).SignedBy(_creator) with
        {
            KeysAdded = [LedgerSigner.PublicKeyOf(LedgerSigner.NewKey())],
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        Db.LedgerStream.AsNoTracking().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Given_Existing_Ledger_Should_Return_Conflict()
    {
        // Arrange
        var ledgerId = Guid.NewGuid();
        var command = LedgerCommands.Create(ledgerId).SignedBy(_creator);
        await _sut.Handle(command, CancellationToken.None);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        Db.LedgerEvent.AsNoTracking().Where(e => e.StreamId == ledgerId).Should().ContainSingle();
    }
}
