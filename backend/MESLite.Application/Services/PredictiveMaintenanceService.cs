using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Services;

/// <summary>
/// Remaining Useful Life (RUL) estimator. Fits a least-squares line to each machine's recent wear
/// readings and projects forward to the maintenance threshold (wear ≈ 90). The slope is the wear
/// rate per hour; RUL = (threshold − currentWear) / rate.
/// </summary>
public sealed class PredictiveMaintenanceService : IPredictiveMaintenanceService
{
    private const double FailureWear = 90.0;
    private const double MaxHours = 1000.0;

    private readonly IApplicationDbContext _db;

    public PredictiveMaintenanceService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<RulDto>> GetRemainingUsefulLifeAsync(int windowMinutes, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var since = now.AddMinutes(-Math.Abs(windowMinutes));

        var machines = await _db.Machines.AsNoTracking()
            .Select(m => new { m.Id, m.Name, m.HealthScore, m.WearLevel })
            .ToListAsync(ct);

        var readings = await _db.TelemetrySnapshots.AsNoTracking()
            .Where(t => t.CreatedAt >= since)
            .Select(t => new { t.MachineId, t.CreatedAt, t.WearLevel })
            .ToListAsync(ct);

        var byMachine = readings
            .GroupBy(r => r.MachineId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CreatedAt).ToList());

        var result = new List<RulDto>();
        foreach (var m in machines)
        {
            byMachine.TryGetValue(m.Id, out var points);
            var wearRate = points is { Count: >= 3 }
                ? SlopePerHour(points.Select(p => ((p.CreatedAt - since).TotalHours, p.WearLevel)))
                : 0;

            double? remainingHours = null;
            DateTime? predictedAt = null;

            if (wearRate > 0.05)
            {
                remainingHours = Math.Clamp((FailureWear - m.WearLevel) / wearRate, 0, MaxHours);
                predictedAt = now.AddHours(remainingHours.Value);
            }

            result.Add(new RulDto(
                m.Id,
                m.Name,
                Math.Round(m.HealthScore, 1),
                Math.Round(m.WearLevel, 1),
                Math.Round(wearRate, 2),
                remainingHours is null ? null : Math.Round(remainingHours.Value, 2),
                predictedAt,
                Classify(m.HealthScore, remainingHours)));
        }

        return result
            .OrderBy(r => r.RemainingHours ?? double.MaxValue)
            .ToList();
    }

    /// <summary>Least-squares slope of y over x (wear per hour).</summary>
    private static double SlopePerHour(IEnumerable<(double X, double Y)> data)
    {
        var pts = data.ToList();
        var n = pts.Count;
        var meanX = pts.Average(p => p.X);
        var meanY = pts.Average(p => p.Y);
        var cov = pts.Sum(p => (p.X - meanX) * (p.Y - meanY));
        var varX = pts.Sum(p => (p.X - meanX) * (p.X - meanX));
        return varX <= 1e-9 ? 0 : cov / varX;
    }

    private static string Classify(double health, double? remainingHours)
    {
        if (health < 30 || (remainingHours is { } h && h < 1)) return "Kritik";
        if (remainingHours is { } hh && hh < 4) return "Uyarı";
        return "İyi";
    }
}
