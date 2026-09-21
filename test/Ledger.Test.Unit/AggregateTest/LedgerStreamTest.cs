using FluentAssertions;
using Ledger.Domain.Aggregates;
using Ledger.Domain.Common;
using Ledger.Domain.Entities;

namespace Ledger.Test.Unit.AggregateTest;

public class LedgerStreamTest
{
    private static readonly byte[] CreatorKey = Bytes(0xA1, 32);
    private static readonly byte[] OtherKey = Bytes(0xB2, 32);
    private static readonly byte[] UnknownKey = Bytes(0xC3, 32);

    private static byte[] Bytes(byte value, int length) => [.. Enumerable.Repeat(value, length)];

    private static Result<LedgerEvent> Append(
        LedgerStream ledger,
        byte[] writeKey,
        byte[] eventHash,
        byte[][]? added = null,
        byte[][]? removed = null,
        int? expectedVersion = null,
        byte[]? previousHash = null)
    {
        return ledger.Append(
            expectedVersion ?? ledger.HeadVersion,
            previousHash ?? ledger.HeadHash,
            eventHash,
            [1, 2, 3],
            writeKey,
            Bytes(0x00, 64),
            added ?? [],
            removed ?? []);
    }

    private static void ShouldBeUnchanged(LedgerStream ledger, int version, byte[] hash, int events, params byte[][] keys)
    {
        ledger.HeadVersion.Should().Be(version);
        ledger.HeadHash.Should().Equal(hash);
        ledger.Events.Should().HaveCount(events);
        ledger.WriteKeys.Should().HaveCount(keys.Length);
        foreach (var key in keys)
            ledger.HasWriteKey(key).Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldStartAtVersionZero_WithCreatorKey()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        ledger.HeadVersion.Should().Be(0);
        ledger.HeadHash.Should().Equal(new byte[32]);
        ledger.Events.Should().BeEmpty();
        ledger.Archived.Should().BeFalse();
        ledger.WriteKeys.Should().ContainSingle();
        ledger.HasWriteKey(CreatorKey).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public void Constructor_ShouldThrow_WhenKeyIsNot32Bytes(int length)
    {
        var act = () => new LedgerStream(Guid.NewGuid(), Bytes(0xA1, length));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Append_ShouldCreateVersionOne_WhenFirstAppend()
    {
        var id = Guid.NewGuid();
        var ledger = new LedgerStream(id, CreatorKey);

        var result = Append(ledger, CreatorKey, Bytes(0x01, 32));

        result.Success.Should().BeTrue();
        var evt = result.RequiredValue;
        evt.Version.Should().Be(1);
        evt.StreamId.Should().Be(id);
        evt.PreviousHash.Should().Equal(new byte[32]);
        evt.WriteKeyPublic.Should().Equal(CreatorKey);
        ledger.Events.Should().ContainSingle().Which.Should().BeSameAs(evt);
    }

    [Fact]
    public void Append_ShouldAdvanceHead_WhenSuccessful()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        var eventHash = Bytes(0x01, 32);

        Append(ledger, CreatorKey, eventHash);

        ledger.HeadVersion.Should().Be(1);
        ledger.HeadHash.Should().Equal(eventHash);
    }

    [Fact]
    public void Append_ShouldChainEvents_WhenAppendedSequentially()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        var firstHash = Bytes(0x01, 32);
        var secondHash = Bytes(0x02, 32);

        var first = Append(ledger, CreatorKey, firstHash);
        var second = Append(ledger, CreatorKey, secondHash);

        first.RequiredValue.Version.Should().Be(1);
        second.RequiredValue.Version.Should().Be(2);
        second.RequiredValue.PreviousHash.Should().Equal(firstHash);
        ledger.HeadVersion.Should().Be(2);
        ledger.HeadHash.Should().Equal(secondHash);
        ledger.Events.Should().HaveCount(2);
    }

    [Fact]
    public void Append_ShouldGrantKey_WhenKeyAdded()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        Append(ledger, CreatorKey, Bytes(0x01, 32), added: [OtherKey]);
        var result = Append(ledger, OtherKey, Bytes(0x02, 32));

        result.Success.Should().BeTrue();
        result.RequiredValue.WriteKeyPublic.Should().Equal(OtherKey);
        ledger.HasWriteKey(OtherKey).Should().BeTrue();
        ledger.WriteKeys.Should().HaveCount(2);
    }

    [Fact]
    public void Append_ShouldRevokeKey_WhenKeyRemoved()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        Append(ledger, CreatorKey, Bytes(0x01, 32), added: [OtherKey]);

        Append(ledger, CreatorKey, Bytes(0x02, 32), removed: [OtherKey]);
        var result = Append(ledger, OtherKey, Bytes(0x03, 32));

        ledger.HasWriteKey(OtherKey).Should().BeFalse();
        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Append_ShouldNotDuplicateKey_WhenKeyAlreadyPresent()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var result = Append(ledger, CreatorKey, Bytes(0x01, 32), added: [CreatorKey, OtherKey, OtherKey]);

        result.Success.Should().BeTrue();
        ledger.WriteKeys.Should().HaveCount(2);
    }

    [Fact]
    public void Append_ShouldAllowKeySwap_WhenRemovingLastKeyAndAddingAnother()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var result = Append(ledger, CreatorKey, Bytes(0x01, 32), added: [OtherKey], removed: [CreatorKey]);

        result.Success.Should().BeTrue();
        ledger.WriteKeys.Should().ContainSingle();
        ledger.HasWriteKey(OtherKey).Should().BeTrue();
        ledger.HasWriteKey(CreatorKey).Should().BeFalse();
    }

    [Fact]
    public void Append_ShouldFailWithNotFound_WhenArchived()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        ledger.Archive(CreatorKey);

        var result = Append(ledger, CreatorKey, Bytes(0x01, 32));

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        ShouldBeUnchanged(ledger, 0, new byte[32], 0, CreatorKey);
    }

    [Fact]
    public void Append_ShouldFailWithForbidden_WhenKeyUnknown()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var result = Append(ledger, UnknownKey, Bytes(0x01, 32));

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        ShouldBeUnchanged(ledger, 0, new byte[32], 0, CreatorKey);
    }

    [Fact]
    public void Append_ShouldFailWithConflict_WhenExpectedVersionIsStale()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        var headHash = Bytes(0x01, 32);
        Append(ledger, CreatorKey, headHash);

        var result = Append(ledger, CreatorKey, Bytes(0x02, 32), expectedVersion: 0);

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        ShouldBeUnchanged(ledger, 1, headHash, 1, CreatorKey);
    }

    [Fact]
    public void Append_ShouldFailWithConflict_WhenPreviousHashDoesNotMatch()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        var headHash = Bytes(0x01, 32);
        Append(ledger, CreatorKey, headHash);

        var result = Append(ledger, CreatorKey, Bytes(0x02, 32), previousHash: Bytes(0xFF, 32));

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        ShouldBeUnchanged(ledger, 1, headHash, 1, CreatorKey);
    }

    [Fact]
    public void Append_ShouldFailWithValidation_WhenRemovingLastKey()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var result = Append(ledger, CreatorKey, Bytes(0x01, 32), removed: [CreatorKey]);

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        ShouldBeUnchanged(ledger, 0, new byte[32], 0, CreatorKey);
    }

    public static TheoryData<string> MalformedInputs =>
    [
        "emptyPayload",
        "shortPreviousHash",
        "shortEventHash",
        "shortWriteKey",
        "shortSignature",
        "shortAddedKey",
        "shortRemovedKey",
    ];

    [Theory]
    [MemberData(nameof(MalformedInputs))]
    public void Append_ShouldThrow_WhenInputIsMalformed(string input)
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var act = () => ledger.Append(
            expectedVersion: 0,
            previousHash: input == "shortPreviousHash" ? Bytes(0x00, 31) : new byte[32],
            eventHash: input == "shortEventHash" ? Bytes(0x01, 31) : Bytes(0x01, 32),
            payload: input == "emptyPayload" ? [] : [1, 2, 3],
            writeKeyPublic: input == "shortWriteKey" ? CreatorKey[..31] : CreatorKey,
            signature: input == "shortSignature" ? Bytes(0x00, 63) : Bytes(0x00, 64),
            keysAdded: input == "shortAddedKey" ? [OtherKey, Bytes(0xD4, 31)] : [],
            keysRemoved: input == "shortRemovedKey" ? [Bytes(0xD4, 31)] : []);

        act.Should().Throw<ArgumentException>();
        ShouldBeUnchanged(ledger, 0, new byte[32], 0, CreatorKey);
    }

    [Fact]
    public void Archive_ShouldArchive_WhenKeyIsValid()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var result = ledger.Archive(CreatorKey);

        result.Success.Should().BeTrue();
        ledger.Archived.Should().BeTrue();
    }

    [Fact]
    public void Archive_ShouldFailWithForbidden_WhenKeyUnknown()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);

        var result = ledger.Archive(UnknownKey);

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        ledger.Archived.Should().BeFalse();
    }

    [Fact]
    public void Archive_ShouldFailWithNotFound_WhenAlreadyArchived()
    {
        var ledger = new LedgerStream(Guid.NewGuid(), CreatorKey);
        ledger.Archive(CreatorKey);

        var result = ledger.Archive(CreatorKey);

        result.Success.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        ledger.Archived.Should().BeTrue();
    }
}
