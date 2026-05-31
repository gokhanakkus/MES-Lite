using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Operators;

/// <summary>
/// GET /api/operators/performance — per operator: total output, attributed downtime and average OEE
/// over the last N days. Downtime/OEE are attributed to the operator who produced the most on each
/// machine in the window (its "primary" operator), avoiding double counting across shared machines.
/// </summary>
public sealed record GetOperatorPerformanceQuery(int Days = 7) : IRequest<IReadOnlyList<OperatorPerformanceDto>>;

public sealed class GetOperatorPerformanceQueryHandler
    : IRequestHandler<GetOperatorPerformanceQuery, IReadOnlyList<OperatorPerformanceDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IOeeCalculationService _oee;

    public GetOperatorPerformanceQueryHandler(IApplicationDbContext db, IOeeCalculationService oee)
    {
        _db = db;
        _oee = oee;
    }

    public async Task<IReadOnlyList<OperatorPerformanceDto>> Handle(GetOperatorPerformanceQuery request, CancellationToken ct)
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-Math.Abs(request.Days));

        var operators = await _db.Operators.AsNoTracking().ToListAsync(ct);

        // Production per (machine, operator) in the window.
        var prod = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.ProducedAt >= from && p.ProducedAt < to && p.OperatorId != null)
            .GroupBy(p => new { p.MachineId, p.OperatorId })
            .Select(g => new { g.Key.MachineId, OperatorId = g.Key.OperatorId!.Value, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync(ct);

        var totalByOperator = prod
            .GroupBy(p => p.OperatorId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

        // Primary operator per machine = the one with the most output.
        var primaryByMachine = prod
            .GroupBy(p => p.MachineId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Qty).First().OperatorId);

        // Downtime minutes per machine in window.
        var downtimes = await _db.Downtimes.AsNoTracking()
            .Where(d => d.StartTime < to && (d.EndTime == null || d.EndTime > from))
            .Select(d => new { d.MachineId, d.StartTime, d.EndTime })
            .ToListAsync(ct);

        var downtimeByOperator = new Dictionary<int, double>();
        foreach (var d in downtimes)
        {
            if (!primaryByMachine.TryGetValue(d.MachineId, out var opId)) continue;
            var start = d.StartTime < from ? from : d.StartTime;
            var end = d.EndTime ?? to;
            if (end > to) end = to;
            var minutes = (end - start).TotalMinutes;
            if (minutes <= 0) continue;
            downtimeByOperator[opId] = downtimeByOperator.GetValueOrDefault(opId) + minutes;
        }

        var oee = await _oee.CalculateForAllAsync(from, to, ct);
        var oeeByOperator = primaryByMachine
            .GroupBy(kv => kv.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(kv => oee.GetValueOrDefault(kv.Key)?.Oee ?? 0).DefaultIfEmpty(0).Average());

        return operators.Select(o => new OperatorPerformanceDto(
            o.Id,
            o.FullName,
            o.Shift.ToString(),
            totalByOperator.GetValueOrDefault(o.Id),
            Math.Round(downtimeByOperator.GetValueOrDefault(o.Id), 1),
            Math.Round(oeeByOperator.GetValueOrDefault(o.Id) * 100, 1)))
            .OrderByDescending(x => x.TotalProduction)
            .ToList();
    }
}
