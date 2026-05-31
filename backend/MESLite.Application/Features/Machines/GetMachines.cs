using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Machines;

/// <summary>GET /api/machines — list every machine enriched with live operator, last-hour output and daily OEE.</summary>
public sealed record GetMachinesQuery : IRequest<IReadOnlyList<MachineDto>>;

public sealed class GetMachinesQueryHandler : IRequestHandler<GetMachinesQuery, IReadOnlyList<MachineDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IOeeCalculationService _oee;

    public GetMachinesQueryHandler(IApplicationDbContext db, IOeeCalculationService oee)
    {
        _db = db;
        _oee = oee;
    }

    public async Task<IReadOnlyList<MachineDto>> Handle(GetMachinesQuery request, CancellationToken ct)
    {
        var machines = await _db.Machines.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct);

        var (from, to) = PeriodType.Daily.ToRange(DateTime.UtcNow);
        var oee = await _oee.CalculateForAllAsync(from, to, ct);

        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var lastHour = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.ProducedAt >= hourAgo)
            .GroupBy(p => p.MachineId)
            .Select(g => new { MachineId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.MachineId, x => x.Qty, ct);

        // Latest operator per machine, via a per-machine correlated subquery (translates cleanly).
        var latestOperators = await _db.Machines.AsNoTracking()
            .Select(m => new
            {
                MachineId = m.Id,
                OperatorName = m.ProductionRecords
                    .Where(p => p.OperatorId != null)
                    .OrderByDescending(p => p.ProducedAt)
                    .Select(p => p.Operator!.FullName)
                    .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.MachineId, x => x.OperatorName, ct);

        return machines.Select(m => new MachineDto(
            m.Id,
            m.Name,
            m.MachineType,
            m.MachineType.ToString(),
            m.Status,
            m.Status.ToString(),
            m.ProductionLine,
            m.IdealRunRatePerHour,
            latestOperators.GetValueOrDefault(m.Id),
            lastHour.GetValueOrDefault(m.Id),
            Math.Round((oee.GetValueOrDefault(m.Id)?.Oee ?? 0) * 100, 1),
            Math.Round(m.HealthScore, 1),
            Math.Round(m.WearLevel, 1),
            m.Rpm,
            Math.Round(m.Speed, 1),
            Math.Round(m.Load, 1),
            Math.Round(m.Vibration, 2),
            Math.Round(m.Temperature, 1),
            Math.Round(m.Efficiency * 100, 1),
            Math.Round(m.CycleTimeSeconds, 2)))
            .ToList();
    }
}
