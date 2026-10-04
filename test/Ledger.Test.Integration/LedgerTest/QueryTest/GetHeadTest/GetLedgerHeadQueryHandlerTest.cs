using FluentAssertions;
using Ledger.Application.Abstractions;
using Ledger.Application.Commands.Append;
using Ledger.Application.Commands.Archive;
using Ledger.Application.Interfaces;
using Ledger.Application.Queries.GetHead;
using Ledger.Domain.Common;
using Ledger.Infrastructure.Persistence.Context;
using Ledger.Test.Integration.Setup;
using Ledger.Test.Integration.Setup.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace Ledger.Test.Integration.LedgerTest.QueryTest.GetHeadTest;

public class GetLedgerHeadQueryHandlerTest : IntegrationTestBase
{
    private readonly GetLedgerHeadQueryHandler _sut;
    private readonly AppendLedgerEventCommandHandler _append;
    private readonly ArchiveLedgerCommandHandler _archive;
    private readonly Key _creator = LedgerSigner.NewKey();

    public GetLedgerHeadQueryHandlerTest(IntegrationTestFactory<Program, LedgerDbContext> factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILedgerRepository>();
        var signatureHelper = scope.ServiceProvider.GetRequiredService<ISignatureHelper>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        _sut = new GetLedgerHeadQueryHandler(repository);
        _append = new AppendLedgerEventCommandHandler(repository, signatureHelper, unitOfWork);
        _archive = new ArchiveLedgerCommandHandler(repository, signatureHelper, unitOfWork);
    }

    [Fact]
    public async Task Handle_Given_Unknown_Ledger_Should_Return_NotFound()
    {
        var result = await _sut.Handle(new GetLedgerHeadQuery { LedgerId = Guid.NewGuid() }, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_Given_New_Ledger_Should_Return_First_Version_And_Hash()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);

        // Act
        var result = await _sut.Handle(new GetLedgerHeadQuery { LedgerId = ledger.Id }, CancellationToken.None);

        // Assert
        var head = result.RequiredValue;
        head.Version.Should().Be(1);
        head.Archived.Should().BeFalse();
        head.Hash.Should().Equal(ledger.Hash);
    }

    [Fact]
    public async Task Handle_Should_Follow_The_Head_After_An_Append()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        byte[] payload = [4, 5, 6];
        await _append.Handle(LedgerCommands.Append(ledger, payload).SignedBy(_creator), CancellationToken.None);

        // Act
        var result = await _sut.Handle(new GetLedgerHeadQuery { LedgerId = ledger.Id }, CancellationToken.None);

        // Assert
        var head = result.RequiredValue;
        head.Version.Should().Be(2);
        head.Hash.Should().Equal(LedgerSigner.Hash(ledger.Id, 2, ledger.Hash, payload));
    }

    [Fact]
    public async Task Handle_Given_Archived_Ledger_Should_Report_Archived()
    {
        // Arrange
        var ledger = await LedgerFactory.SeedAsync(Db, _creator);
        await _archive.Handle(LedgerCommands.Archive(ledger).SignedBy(_creator), CancellationToken.None);

        // Act
        var result = await _sut.Handle(new GetLedgerHeadQuery { LedgerId = ledger.Id }, CancellationToken.None);

        // Assert
        result.RequiredValue.Archived.Should().BeTrue();
        result.RequiredValue.Version.Should().Be(2);
    }
}
