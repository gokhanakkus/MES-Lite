using MESLite.Application.Common.Dtos;

namespace MESLite.Application.Common.Interfaces;

/// <summary>
/// Estimates Remaining Useful Life (RUL) per machine by fitting the recent wear trend and
/// projecting forward to the maintenance threshold.
/// </summary>
public interface IPredictiveMaintenanceService
{
    Task<IReadOnlyList<RulDto>> GetRemainingUsefulLifeAsync(int windowMinutes, CancellationToken ct = default);
}
