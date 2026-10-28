using FluentAssertions;
using Ledger.Application.Commands.Append;
using Ledger.Application.Common;
using Ledger.Domain.Common;

namespace Ledger.Test.Unit.ValidatorTest;

public class AppendLedgerEventCommandValidatorTest
{
    private readonly AppendLedgerEventCommandValidator _sut = new();

    private static byte[] Bytes(byte value, int length) => [.. Enumerable.Repeat(value, length)];

    private static List<byte[]> Keys(int count) => [.. Enumerable.Range(0, count).Select(i => Bytes((byte)i, 32))];

    private static AppendLedgerEventCommand Command(int payloadBytes = 3, int keysAdded = 0, int keysRemoved = 0, int inviteKeysAdded = 0) => new()
    {
        LedgerId = Guid.NewGuid(),
        ExpectedVersion = 1,
        PreviousHash = Bytes(0x03, 32),
        Payload = Bytes(0x04, payloadBytes),
        WriteKeyPublic = Bytes(0x01, 32),
        Signature = Bytes(0x02, 64),
        KeyChanges = new KeyChanges(Keys(keysAdded), Keys(inviteKeysAdded), Keys(keysRemoved)),
    };

    [Fact]
    public void Validate_Given_Payload_At_Limit_Should_Be_Valid()
    {
        // Act
        var result = _sut.Validate(Command(payloadBytes: LedgerQueryLimits.MaxPayloadBytes));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Given_Payload_Over_Limit_Should_Be_Invalid()
    {
        // Act
        var result = _sut.Validate(Command(payloadBytes: LedgerQueryLimits.MaxPayloadBytes + 1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(AppendLedgerEventCommand.Payload));
    }

    [Fact]
    public void Validate_Given_Keys_At_Limit_Should_Be_Valid()
    {
        // Act
        var result = _sut.Validate(Command(keysAdded: LedgerQueryLimits.MaxKeysPerEvent, keysRemoved: LedgerQueryLimits.MaxKeysPerEvent));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Given_Too_Many_Keys_Added_Should_Be_Invalid()
    {
        // Act
        var result = _sut.Validate(Command(keysAdded: LedgerQueryLimits.MaxKeysPerEvent + 1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.Added");
    }

    [Fact]
    public void Validate_Given_Too_Many_Keys_Removed_Should_Be_Invalid()
    {
        // Act
        var result = _sut.Validate(Command(keysRemoved: LedgerQueryLimits.MaxKeysPerEvent + 1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.Removed");
    }

    [Fact]
    public void Validate_Given_Invite_Keys_At_Limit_Should_Be_Valid()
    {
        // Act
        var result = _sut.Validate(Command(inviteKeysAdded: LedgerQueryLimits.MaxKeysPerEvent));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Given_Too_Many_Invite_Keys_Should_Be_Invalid()
    {
        // Act
        var result = _sut.Validate(Command(inviteKeysAdded: LedgerQueryLimits.MaxKeysPerEvent + 1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("KeyChanges.InvitesAdded");
    }

    [Fact]
    public void Validate_Given_Malformed_Invite_Key_Should_Be_Invalid()
    {
        // Arrange
        var command = Command() with { KeyChanges = new KeyChanges([], [Bytes(0x10, 31)], []) };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().StartWith("KeyChanges.InvitesAdded");
    }
}
