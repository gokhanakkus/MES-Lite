namespace MESLite.Application.Common.Interfaces;

/// <summary>
/// Push channel for real-time shop-floor events. Implemented in the API layer over SignalR,
/// but abstracted here so Application/Simulator code never depends on SignalR directly.
/// </summary>
public interface IProductionNotifier
{
    Task MachineStatusChangedAsync(object payload, CancellationToken ct = default);
    Task ProductionUpdatedAsync(object payload, CancellationToken ct = default);
    Task DowntimeCreatedAsync(object payload, CancellationToken ct = default);
    Task OeeUpdatedAsync(object payload, CancellationToken ct = default);

    /// <summary>Live physical telemetry (health, rpm, load, vibration, temperature, wear) per machine.</summary>
    Task MachineTelemetryAsync(object payload, CancellationToken ct = default);

    /// <summary>A new anomaly/alarm was raised.</summary>
    Task AlarmRaisedAsync(object payload, CancellationToken ct = default);

    /// <summary>An active alarm cleared.</summary>
    Task AlarmResolvedAsync(object payload, CancellationToken ct = default);
}
