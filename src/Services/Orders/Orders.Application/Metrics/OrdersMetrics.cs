using System.Diagnostics.Metrics;

namespace Orders.Application.Metrics;

public sealed class OrdersMetrics
{
    public const string MeterName = "Orders.API";

    private readonly Counter<long> _ordersPlaced;
    private readonly Counter<long> _ordersConfirmed;
    private readonly Counter<long> _ordersFailed;
    private readonly Histogram<double> _orderTotalAmount;

    public OrdersMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _ordersPlaced = meter.CreateCounter<long>(
            "orders.placed.total",
            description: "Total number of orders placed");

        _ordersConfirmed = meter.CreateCounter<long>(
            "orders.confirmed.total",
            description: "Total number of orders confirmed");

        _ordersFailed = meter.CreateCounter<long>(
            "orders.failed.total",
            description: "Total number of orders failed");

        _orderTotalAmount = meter.CreateHistogram<double>(
            "orders.total_amount",
            unit: "USD",
            description: "Distribution of order total amounts");
    }

    public void OrderPlaced(decimal totalAmount)
    {
        _ordersPlaced.Add(1);
        _orderTotalAmount.Record((double)totalAmount);
    }

    public void OrderConfirmed() => _ordersConfirmed.Add(1);

    public void OrderFailed() => _ordersFailed.Add(1);
}
