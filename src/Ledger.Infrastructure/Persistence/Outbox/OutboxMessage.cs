namespace Ledger.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; init; }
}