namespace Shared.Contracts.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string CorrelationId { get; }
    int Version { get; }
}
