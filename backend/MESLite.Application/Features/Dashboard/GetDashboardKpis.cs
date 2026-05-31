using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Models;
using MESLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Dashboard;

/// <summary>Top KPI cards: machine counts by status, today's output, plant average OEE.</summary>
public sealed record GetDashboardKpisQuery : IRequest<DashboardKpiDto>;

public sealed class GetDashboardKpisQueryHandler : IRequestHandler<GetDashboardKpisQuery, DashboardKpiDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IOeeCalculationService _oee;

    public GetDashboardKpisQueryHandler(IApplicationDbContext db, IOeeCalculationService oee)
    {
        _db = db;
        _oee = oee;
    }

    public async Task<DashboardKpiDto> Handle(GetDashboardKpisQuery request, CancellationToken ct)
    {
        var statusCounts = await _db.Machines.AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = statusCounts.Sum(x => x.Count);
        int CountFor(MachineStatus s) => statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        // "Today" = calendar day (UTC) for the production KPI; OEE uses the rolling daily window.
        var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var todayProduction = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.ProducedAt >= todayStart)
            .SumAsync(p => (int?)p.Quantity, ct) ?? 0;

        var (from, to) = PeriodType.Daily.ToRange(DateTime.UtcNow);
        var oee = await _oee.CalculateForAllAsync(from, to, ct);
        var avgOee = oee.Count == 0 ? 0 : Math.Round(oee.Values.Average(r => r.Oee) * 100, 1);

        return new DashboardKpiDto(
            total,
            CountFor(MachineStatus.Running),
            CountFor(MachineStatus.Stopped),
            CountFor(MachineStatus.Maintenance),
            todayProduction,
            avgOee);
    }
}
