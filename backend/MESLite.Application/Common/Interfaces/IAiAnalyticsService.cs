using MESLite.Application.Common.Dtos;

namespace MESLite.Application.Common.Interfaces;

/// <summary>
/// Rule-based "AI" analytics over recent downtime history: surfaces the most problematic
/// machines and generates preventive-maintenance recommendations.
/// </summary>
public interface IAiAnalyticsService
{
    Task<IReadOnlyList<MaintenanceInsightDto>> GetMaintenanceInsightsAsync(int days, CancellationToken ct = default);
}
