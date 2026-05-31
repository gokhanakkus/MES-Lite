using MESLite.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace MESLite.API.Hubs;

/// <summary>SignalR-backed implementation of <see cref="IProductionNotifier"/>.</summary>
public sealed class SignalRProductionNotifier : IProductionNotifier
{
    private readonly IHubContext<ProductionHub> _hub;

    public SignalRProductionNotifier(IHubContext<ProductionHub> hub) => _hub = hub;

    public Task MachineStatusChangedAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.MachineStatusChanged, payload, ct);

    public Task ProductionUpdatedAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.ProductionUpdated, payload, ct);

    public Task DowntimeCreatedAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.DowntimeCreated, payload, ct);

    public Task OeeUpdatedAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.OeeUpdated, payload, ct);

    public Task MachineTelemetryAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.MachineTelemetry, payload, ct);

    public Task AlarmRaisedAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.AlarmRaised, payload, ct);

    public Task AlarmResolvedAsync(object payload, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync(ProductionHub.AlarmResolved, payload, ct);
}
