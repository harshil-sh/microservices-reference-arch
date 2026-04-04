namespace Shared.Contracts.Events;

public record OrderConfirmed : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    public int Version { get; init; } = 1;

    public Guid OrderId { get; init; }
    public DateTime ConfirmedAt { get; init; }
}
