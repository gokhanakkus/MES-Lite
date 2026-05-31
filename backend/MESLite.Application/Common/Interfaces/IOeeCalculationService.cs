using MESLite.Application.Common.Oee;

namespace MESLite.Application.Common.Interfaces;

/// <summary>
/// Computes OEE from persisted production/downtime/quality data for a given window.
/// </summary>
public interface IOeeCalculationService
{
    /// <summary>OEE for a single machine over [from, to).</summary>
    Task<OeeResult> CalculateAsync(int machineId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>OEE for every machine over [from, to), keyed by machine id.</summary>
    Task<IReadOnlyDictionary<int, OeeResult>> CalculateForAllAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
