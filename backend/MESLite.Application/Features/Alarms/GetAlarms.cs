using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Alarms;

/// <summary>GET /api/alarms — recent alarms (optionally only active ones), newest first.</summary>
public sealed record GetAlarmsQuery(bool ActiveOnly = false, int Hours = 24, int Take = 100)
    : IRequest<IReadOnlyList<AlarmDto>>;

public sealed class GetAlarmsQueryHandler : IRequestHandler<GetAlarmsQuery, IReadOnlyList<AlarmDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAlarmsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AlarmDto>> Handle(GetAlarmsQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Abs(request.Hours));
        var take = Math.Clamp(request.Take, 1, 500);

        var query = _db.Alarms.AsNoTracking()
            .Where(a => a.RaisedAt >= since || a.ResolvedAt == null);

        if (request.ActiveOnly)
            query = query.Where(a => a.ResolvedAt == null);

        return await query
            .OrderByDescending(a => a.RaisedAt)
            .Take(take)
            .Select(a => new AlarmDto(
                a.Id, a.MachineId, a.Machine.Name,
                a.Metric.ToString(), a.Severity.ToString(), a.Detector.ToString(),
                a.Message, a.Value, a.Limit, a.RaisedAt, a.ResolvedAt, a.ResolvedAt == null))
            .ToListAsync(ct);
    }
}
