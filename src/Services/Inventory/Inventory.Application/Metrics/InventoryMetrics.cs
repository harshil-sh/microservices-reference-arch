using System.Diagnostics.Metrics;

namespace Inventory.Application.Metrics;

public sealed class InventoryMetrics
{
    public const string MeterName = "Inventory.API";

    private readonly Counter<long> _stockReservations;
    private readonly Counter<long> _stockInsufficient;
    private readonly Counter<long> _stockRollbacks;

    public InventoryMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _stockReservations = meter.CreateCounter<long>(
            "inventory.stock_reservations.total",
            description: "Total number of successful stock reservations");

        _stockInsufficient = meter.CreateCounter<long>(
            "inventory.stock_insufficient.total",
            description: "Total number of stock insufficient events");

        _stockRollbacks = meter.CreateCounter<long>(
            "inventory.stock_rollbacks.total",
            description: "Total number of partial reservation rollbacks");
    }

    public void StockReserved() => _stockReservations.Add(1);

    public void StockInsufficient() => _stockInsufficient.Add(1);

    public void StockRollback() => _stockRollbacks.Add(1);
}
