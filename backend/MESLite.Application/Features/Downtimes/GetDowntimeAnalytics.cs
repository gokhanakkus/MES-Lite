using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Downtimes;

/// <summary>GET /api/downtimes — reason distribution, per-machine totals and recent stops over a window.</summary>
public sealed record GetDowntimeAnalyticsQuery(int Days = 7) : IRequest<DowntimeAnalyticsDto>;

public sealed class GetDowntimeAnalyticsQueryHandler : IRequestHandler<GetDowntimeAnalyticsQuery, DowntimeAnalyticsDto>
{
    private readonly IApplicationDbContext _db;

    public GetDowntimeAnalyticsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DowntimeAnalyticsDto> Handle(GetDowntimeAnalyticsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(request.Days));

        var rows = await _db.Downtimes.AsNoTracking()
            .Where(d => d.StartTime >= since)
            .Select(d => new
            {
                d.Id,
                d.MachineId,
                MachineName = d.Machine.Name,
                d.Reason,
                d.StartTime,
                d.EndTime
            })
            .ToListAsync(ct);

        var byReason = rows
            .GroupBy(r => r.Reason)
            .Select(g => new DefectByReasonDto(g.Key.ToString(), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var byMachine = rows
            .GroupBy(r => new { r.MachineId, r.MachineName })
            .Select(g => new MachineDowntimeDto(
                g.Key.MachineId,
                g.Key.MachineName,
                g.Count(),
                Math.Round(g.Sum(d => ((d.EndTime ?? DateTime.UtcNow) - d.StartTime).TotalMinutes), 1)))
            .OrderByDescending(x => x.TotalMinutes)
            .ToList();

        var recent = rows
            .OrderByDescending(r => r.StartTime)
            .Take(50)
            .Select(r => new DowntimeDto(
                r.Id, r.MachineId, r.MachineName, r.Reason, r.Reason.ToString(),
                r.StartTime, r.EndTime,
                Math.Round(((r.EndTime ?? DateTime.UtcNow) - r.StartTime).TotalMinutes, 1),
                r.EndTime is null))
            .ToList();

        return new DowntimeAnalyticsDto(byReason, byMachine, recent);
    }
}
