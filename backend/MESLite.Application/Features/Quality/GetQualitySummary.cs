using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Quality;

/// <summary>GET /api/quality — total production, defects, quality % and per-machine defect breakdown.</summary>
public sealed record GetQualitySummaryQuery(int Days = 7) : IRequest<QualitySummaryDto>;

public sealed class GetQualitySummaryQueryHandler : IRequestHandler<GetQualitySummaryQuery, QualitySummaryDto>
{
    private readonly IApplicationDbContext _db;

    public GetQualitySummaryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<QualitySummaryDto> Handle(GetQualitySummaryQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(request.Days));

        var rows = await _db.QualityRecords.AsNoTracking()
            .Where(q => q.CreatedAt >= since)
            .Select(q => new
            {
                q.Id,
                q.MachineId,
                MachineName = q.Machine.Name,
                q.ProducedQuantity,
                q.DefectQuantity,
                q.CreatedAt
            })
            .ToListAsync(ct);

        var totalProduced = rows.Sum(r => r.ProducedQuantity);
        var totalDefects = rows.Sum(r => r.DefectQuantity);
        var qualityPct = totalProduced == 0 ? 0 : Math.Round((double)(totalProduced - totalDefects) / totalProduced * 100, 2);

        var byMachine = rows
            .GroupBy(r => r.MachineName)
            .Select(g => new DefectByReasonDto(g.Key, g.Sum(x => x.DefectQuantity)))
            .OrderByDescending(x => x.Count)
            .ToList();

        var recent = rows
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .Select(r => new QualityRecordDto(
                r.Id, r.MachineId, r.MachineName, r.ProducedQuantity, r.DefectQuantity,
                r.ProducedQuantity == 0 ? 0 : Math.Round((double)(r.ProducedQuantity - r.DefectQuantity) / r.ProducedQuantity * 100, 2),
                r.CreatedAt))
            .ToList();

        return new QualitySummaryDto(totalProduced, totalDefects, qualityPct, byMachine, recent);
    }
}
