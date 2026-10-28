using FluentAssertions;
using Ledger.Application.Commands.Create;
using Ledger.Application.Common;
using Ledger.Domain.Common;

namespace Ledger.Test.Unit.ValidatorTest;

public class CreateLedgerCommandValidatorTest
{
    private readonly CreateLedgerCommandValidator _sut = new();

    private static byte[] Bytes(byte value, int length) => [.. Enumerable.Repeat(value, length)];

    private static CreateLedgerCommand Command(int keysAdded) => new()
    {
        LedgerId = Guid.NewGuid(),
        Payload = [1, 2, 3],
        WriteKey = Bytes(0x01, 32),
        Signature = Bytes(0x02, 64),
        KeyChanges = new KeyChanges([], [.. Enumerable.Range(0, keysAdded).Select(i => Bytes((byte)(0x10 + i), 32))], []),
    };

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void Validate_Given_At_Least_Two_Invite_Keys_Should_Be_Valid(int keysAdded)
    {
        // Act
        var result = _sut.Validate(Command(keysAdded));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Validate_Given_Fewer_Than_Two_Invite_Keys_Should_Be_Invalid(int keysAdded)
    {
        // Act
        var result = _sut.Validate(Command(keysAdded));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.InvitesAdded");
    }

    [Fact]
    public void Validate_Given_Malformed_Key_Added_Should_Be_Invalid()
    {
        // Arrange
        var command = Command(3) with { KeyChanges = new KeyChanges([], [Bytes(0x10, 32), Bytes(0x11, 32), Bytes(0x12, 31)], []) };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().StartWith("KeyChanges.InvitesAdded");
    }

    [Fact]
    public void Validate_Given_Payload_Over_Limit_Should_Be_Invalid()
    {
        // Arrange
        var command = Command(3) with { Payload = Bytes(0x04, LedgerQueryLimits.MaxPayloadBytes + 1) };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(CreateLedgerCommand.Payload));
    }

    [Fact]
    public void Validate_Given_Too_Many_Keys_Added_Should_Be_Invalid()
    {
        // Act
        var result = _sut.Validate(Command(LedgerQueryLimits.MaxKeysPerEvent + 1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.InvitesAdded");
    }

    [Fact]
    public void Validate_Given_Writer_Keys_Added_Should_Be_Invalid()
    {
        // Arrange
        var command = Command(2) with { KeyChanges = Command(2).KeyChanges with { Added = [Bytes(0x20, 32)] } };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.Added");
    }

    [Fact]
    public void Validate_Given_Keys_Removed_Should_Be_Invalid()
    {
        // Arrange
        var command = Command(2) with { KeyChanges = Command(2).KeyChanges with { Removed = [Bytes(0x20, 32)] } };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.Removed");
    }
}
