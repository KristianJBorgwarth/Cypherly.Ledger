using FluentAssertions;
using Ledger.Application.Commands.Archive;
using Ledger.Application.Common;

namespace Ledger.Test.Unit.ValidatorTest;

public class ArchiveLedgerCommandValidatorTest
{
    private readonly ArchiveLedgerCommandValidator _sut = new();

    private static byte[] Bytes(byte value, int length) => [.. Enumerable.Repeat(value, length)];

    private static ArchiveLedgerCommand Command(int payloadBytes) => new()
    {
        LedgerId = Guid.NewGuid(),
        ExpectedVersion = 1,
        PreviousHash = Bytes(0x03, 32),
        Payload = Bytes(0x04, payloadBytes),
        WriteKeyPublic = Bytes(0x01, 32),
        Signature = Bytes(0x02, 64),
    };

    [Fact]
    public void Validate_Given_Payload_At_Limit_Should_Be_Valid()
    {
        // Act
        var result = _sut.Validate(Command(LedgerQueryLimits.MaxPayloadBytes));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Given_Payload_Over_Limit_Should_Be_Invalid()
    {
        // Act
        var result = _sut.Validate(Command(LedgerQueryLimits.MaxPayloadBytes + 1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(ArchiveLedgerCommand.Payload));
    }
}
