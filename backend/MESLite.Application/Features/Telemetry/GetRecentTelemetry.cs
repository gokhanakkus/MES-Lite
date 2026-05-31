using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Telemetry;

/// <summary>GET /api/telemetry/recent — recent telemetry snapshots for trend sparklines/charts.
/// Optionally scoped to a single machine.</summary>
public sealed record GetRecentTelemetryQuery(int Minutes = 30, int? MachineId = null)
    : IRequest<IReadOnlyList<TelemetryReadingDto>>;

public sealed class GetRecentTelemetryQueryHandler
    : IRequestHandler<GetRecentTelemetryQuery, IReadOnlyList<TelemetryReadingDto>>
{
    private readonly IApplicationDbContext _db;

    public GetRecentTelemetryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TelemetryReadingDto>> Handle(GetRecentTelemetryQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddMinutes(-Math.Abs(request.Minutes));

        return await _db.TelemetrySnapshots.AsNoTracking()
            .Where(t => t.CreatedAt >= since && (request.MachineId == null || t.MachineId == request.MachineId))
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TelemetryReadingDto(
                t.MachineId, t.CreatedAt, t.HealthScore, t.WearLevel,
                t.Vibration, t.Temperature, t.Rpm, t.Speed, t.Load))
            .ToListAsync(ct);
    }
}
