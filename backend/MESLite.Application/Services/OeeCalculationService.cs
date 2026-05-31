using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Oee;
using MESLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Services;

/// <summary>
/// Aggregates persisted data for a window and feeds <see cref="OeeCalculator"/>.
/// Downtime overlap is clamped to the window so partial / ongoing stops are counted fairly.
/// </summary>
public sealed class OeeCalculationService : IOeeCalculationService
{
    private readonly IApplicationDbContext _db;

    public OeeCalculationService(IApplicationDbContext db) => _db = db;

    public async Task<OeeResult> CalculateAsync(int machineId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var machine = await _db.Machines.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == machineId, ct);
        if (machine is null)
        {
            return OeeResult.Empty;
        }

        var produced = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.MachineId == machineId && p.ProducedAt >= from && p.ProducedAt < to)
            .SumAsync(p => (int?)p.Quantity, ct) ?? 0;

        var qualityQuery = _db.QualityRecords.AsNoTracking()
            .Where(q => q.MachineId == machineId && q.CreatedAt >= from && q.CreatedAt < to);
        var qualityProduced = await qualityQuery.SumAsync(q => (int?)q.ProducedQuantity, ct) ?? 0;
        var qualityDefects = await qualityQuery.SumAsync(q => (int?)q.DefectQuantity, ct) ?? 0;

        var downtimes = await _db.Downtimes.AsNoTracking()
            .Where(d => d.MachineId == machineId && d.StartTime < to && (d.EndTime == null || d.EndTime > from))
            .ToListAsync(ct);

        var downtimeMinutes = downtimes.Sum(d => OverlapMinutes(d, from, to));
        var plannedMinutes = (to - from).TotalMinutes;

        return OeeCalculator.Compute(
            plannedMinutes,
            downtimeMinutes,
            produced,
            machine.IdealRunRatePerHour,
            qualityProduced > 0 ? qualityProduced : produced,
            qualityDefects);
    }

    public async Task<IReadOnlyDictionary<int, OeeResult>> CalculateForAllAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var plannedMinutes = (to - from).TotalMinutes;

        var machines = await _db.Machines.AsNoTracking()
            .Select(m => new { m.Id, m.IdealRunRatePerHour })
            .ToListAsync(ct);

        var production = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.ProducedAt >= from && p.ProducedAt < to)
            .GroupBy(p => p.MachineId)
            .Select(g => new { MachineId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.MachineId, x => x.Qty, ct);

        var quality = await _db.QualityRecords.AsNoTracking()
            .Where(q => q.CreatedAt >= from && q.CreatedAt < to)
            .GroupBy(q => q.MachineId)
            .Select(g => new { MachineId = g.Key, Produced = g.Sum(x => x.ProducedQuantity), Defects = g.Sum(x => x.DefectQuantity) })
            .ToDictionaryAsync(x => x.MachineId, x => new { x.Produced, x.Defects }, ct);

        var downtimes = await _db.Downtimes.AsNoTracking()
            .Where(d => d.StartTime < to && (d.EndTime == null || d.EndTime > from))
            .ToListAsync(ct);
        var downtimeByMachine = downtimes
            .GroupBy(d => d.MachineId)
            .ToDictionary(g => g.Key, g => g.Sum(d => OverlapMinutes(d, from, to)));

        var result = new Dictionary<int, OeeResult>();
        foreach (var m in machines)
        {
            var producedQty = production.GetValueOrDefault(m.Id);
            quality.TryGetValue(m.Id, out var q);
            var downtimeMinutes = downtimeByMachine.GetValueOrDefault(m.Id);

            result[m.Id] = OeeCalculator.Compute(
                plannedMinutes,
                downtimeMinutes,
                producedQty,
                m.IdealRunRatePerHour,
                q?.Produced ?? producedQty,
                q?.Defects ?? 0);
        }

        return result;
    }

    /// <summary>Minutes a downtime overlaps with [from, to); ongoing stops are bounded at "now".</summary>
    private static double OverlapMinutes(Downtime d, DateTime from, DateTime to)
    {
        var start = d.StartTime < from ? from : d.StartTime;
        var end = d.EndTime ?? DateTime.UtcNow;
        if (end > to) end = to;
        var minutes = (end - start).TotalMinutes;
        return minutes > 0 ? minutes : 0;
    }
}
