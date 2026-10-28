namespace Ledger.Domain.Common;

public sealed record KeyChanges(
    IReadOnlyCollection<byte[]> Added,
    IReadOnlyCollection<byte[]> InvitesAdded,
    IReadOnlyCollection<byte[]> Removed)
{
    public static readonly KeyChanges None = new([], [], []);
}
