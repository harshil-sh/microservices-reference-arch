using System.Diagnostics.Metrics;

namespace Notifications.Worker.Metrics;

public sealed class NotificationsMetrics
{
    public const string MeterName = "Notifications.Worker";

    private readonly Counter<long> _notificationsSent;

    public NotificationsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _notificationsSent = meter.CreateCounter<long>(
            "notifications.sent.total",
            description: "Total number of notifications sent");
    }

    public void NotificationSent(string channel)
    {
        _notificationsSent.Add(1, new KeyValuePair<string, object?>("channel", channel));
    }
}
