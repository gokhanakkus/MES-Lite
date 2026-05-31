using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Reports;

/// <summary>
/// Builds a per-machine report (production, defects, OEE breakdown, downtime) for a period.
/// Consumed by JSON, CSV and Excel endpoints.
/// </summary>
public sealed record GetReportQuery(PeriodType Period = PeriodType.Daily) : IRequest<ReportDto>;

public sealed class GetReportQueryHandler : IRequestHandler<GetReportQuery, ReportDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IOeeCalculationService _oee;

    public GetReportQueryHandler(IApplicationDbContext db, IOeeCalculationService oee)
    {
        _db = db;
        _oee = oee;
    }

    public async Task<ReportDto> Handle(GetReportQuery request, CancellationToken ct)
    {
        var (from, to) = request.Period.ToRange(DateTime.UtcNow);

        var machines = await _db.Machines.AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, m.Name, m.MachineType })
            .ToListAsync(ct);

        var oee = await _oee.CalculateForAllAsync(from, to, ct);

        var production = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.ProducedAt >= from && p.ProducedAt < to)
            .GroupBy(p => p.MachineId)
            .Select(g => new { MachineId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.MachineId, x => x.Qty, ct);

        var defects = await _db.QualityRecords.AsNoTracking()
            .Where(q => q.CreatedAt >= from && q.CreatedAt < to)
            .GroupBy(q => q.MachineId)
            .Select(g => new { MachineId = g.Key, Defects = g.Sum(x => x.DefectQuantity) })
            .ToDictionaryAsync(x => x.MachineId, x => x.Defects, ct);

        var rows = machines.Select(m =>
        {
            var r = oee.GetValueOrDefault(m.Id);
            return new ReportRowDto(
                m.Id,
                m.Name,
                m.MachineType.ToString(),
                production.GetValueOrDefault(m.Id),
                defects.GetValueOrDefault(m.Id),
                Pct(r?.Availability),
                Pct(r?.Performance),
                Pct(r?.Quality),
                Pct(r?.Oee),
                Math.Round((r?.DowntimeMinutes ?? 0) / 60.0, 2));
        }).ToList();

        return new ReportDto(request.Period.ToString(), from, to, rows);
    }

    private static double Pct(double? ratio) => Math.Round((ratio ?? 0) * 100, 1);
}
