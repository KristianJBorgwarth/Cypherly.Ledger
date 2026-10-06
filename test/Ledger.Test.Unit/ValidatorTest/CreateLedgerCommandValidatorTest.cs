using FluentAssertions;
using Ledger.Application.Commands.Create;

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
        KeysAdded = [.. Enumerable.Range(0, keysAdded).Select(i => Bytes((byte)(0x10 + i), 32))],
    };

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Validate_Given_At_Least_Three_Keys_Added_Should_Be_Valid(int keysAdded)
    {
        // Act
        var result = _sut.Validate(Command(keysAdded));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Validate_Given_Fewer_Than_Three_Keys_Added_Should_Be_Invalid(int keysAdded)
    {
        // Act
        var result = _sut.Validate(Command(keysAdded));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(nameof(CreateLedgerCommand.KeysAdded));
    }

    [Fact]
    public void Validate_Given_Malformed_Key_Added_Should_Be_Invalid()
    {
        // Arrange
        var command = Command(3) with { KeysAdded = [Bytes(0x10, 32), Bytes(0x11, 32), Bytes(0x12, 31)] };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().StartWith(nameof(CreateLedgerCommand.KeysAdded));
    }
}
