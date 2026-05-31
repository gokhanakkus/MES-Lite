using Microsoft.AspNetCore.SignalR;

namespace MESLite.API.Hubs;

/// <summary>
/// Real-time channel for shop-floor events. The frontend subscribes here; the simulator
/// (via <see cref="MESLite.Application.Common.Interfaces.IProductionNotifier"/>) pushes updates.
/// </summary>
public sealed class ProductionHub : Hub
{
    // Event names shared with the TypeScript client.
    public const string MachineStatusChanged = "MachineStatusChanged";
    public const string ProductionUpdated = "ProductionUpdated";
    public const string DowntimeCreated = "DowntimeCreated";
    public const string OeeUpdated = "OeeUpdated";
    public const string MachineTelemetry = "MachineTelemetry";
    public const string AlarmRaised = "AlarmRaised";
    public const string AlarmResolved = "AlarmResolved";
}
