using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using MESLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Services;

/// <summary>
/// Heuristic analytics engine. Looks at the last N days of downtime and, per machine,
/// determines the dominant failure mode, scores severity, and emits a human-readable
/// preventive-maintenance recommendation (Turkish, matching the spec example).
/// </summary>
public sealed class AiAnalyticsService : IAiAnalyticsService
{
    private readonly IApplicationDbContext _db;

    public AiAnalyticsService(IApplicationDbContext db) => _db = db;

    private static readonly IReadOnlyDictionary<DowntimeReason, string> ReasonText = new Dictionary<DowntimeReason, string>
    {
        [DowntimeReason.YarnBreak] = "YarnBreak (iplik kopması)",
        [DowntimeReason.MaterialWaiting] = "MaterialWaiting (malzeme bekleme)",
        [DowntimeReason.Maintenance] = "Maintenance (bakım)",
        [DowntimeReason.OperatorWaiting] = "OperatorWaiting (operatör bekleme)",
        [DowntimeReason.PowerFailure] = "PowerFailure (elektrik kesintisi)"
    };

    public async Task<IReadOnlyList<MaintenanceInsightDto>> GetMaintenanceInsightsAsync(int days, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));

        var downtimes = await _db.Downtimes.AsNoTracking()
            .Where(d => d.StartTime >= since)
            .Select(d => new
            {
                d.MachineId,
                MachineName = d.Machine.Name,
                d.Reason,
                d.StartTime,
                d.EndTime
            })
            .ToListAsync(ct);

        var insights = new List<MaintenanceInsightDto>();

        foreach (var group in downtimes.GroupBy(d => new { d.MachineId, d.MachineName }))
        {
            var stops = group.Count();
            var totalHours = group.Sum(d => ((d.EndTime ?? DateTime.UtcNow) - d.StartTime).TotalHours);

            var topReasonGroup = group
                .GroupBy(d => d.Reason)
                .OrderByDescending(g => g.Count())
                .First();

            var reason = topReasonGroup.Key;
            var reasonCount = topReasonGroup.Count();
            var severity = ScoreSeverity(stops, reasonCount, totalHours);

            insights.Add(new MaintenanceInsightDto(
                group.Key.MachineId,
                group.Key.MachineName,
                ReasonText.GetValueOrDefault(reason, reason.ToString()),
                reasonCount,
                stops,
                Math.Round(totalHours, 1),
                severity,
                BuildRecommendation(group.Key.MachineName, reason, reasonCount, days, severity)));
        }

        // Most problematic machines first.
        return insights
            .OrderByDescending(i => i.Severity == "Yüksek" ? 2 : i.Severity == "Orta" ? 1 : 0)
            .ThenByDescending(i => i.TopReasonCount)
            .ToList();
    }

    private static string ScoreSeverity(int stops, int topReasonCount, double totalHours)
    {
        if (topReasonCount >= 12 || totalHours >= 20) return "Yüksek";
        if (topReasonCount >= 5 || totalHours >= 8) return "Orta";
        return "Düşük";
    }

    private static string BuildRecommendation(string machine, DowntimeReason reason, int count, int days, string severity)
    {
        var advice = reason switch
        {
            DowntimeReason.YarnBreak => "İplik gerginliği ve mekik ayarları kontrol edilmeli; önleyici bakım önerilir.",
            DowntimeReason.MaterialWaiting => "Malzeme tedarik akışı ve hat besleme planı gözden geçirilmeli.",
            DowntimeReason.Maintenance => "Bakım periyotları sıklaştırılmalı; yedek parça stoğu artırılmalı.",
            DowntimeReason.OperatorWaiting => "Vardiya personel planlaması ve operatör atamaları optimize edilmeli.",
            DowntimeReason.PowerFailure => "Elektrik altyapısı ve UPS/jeneratör yedekliliği denetlenmeli.",
            _ => "Genel bakım önerilir."
        };

        return $"{machine} son {days} günde {count} kez {reason} nedeniyle durmuştur. " +
               $"Önem derecesi: {severity}. {advice}";
    }
}
