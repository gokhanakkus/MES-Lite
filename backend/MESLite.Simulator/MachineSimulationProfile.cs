using MESLite.Domain.Enums;

namespace MESLite.Simulator;

/// <summary>
/// Per-machine-type behaviour for the simulator: stoppage likelihood, defect rate and which
/// downtime reasons are plausible (with weights). Mirrors the real-world scenario in the spec.
/// </summary>
public sealed record MachineSimulationProfile(
    double StopProbability,
    double DefectRate,
    double MaintenanceProbability,
    int RpmMin,
    int RpmMax,
    double BaseVibration,
    double WearPerTick,
    IReadOnlyList<(DowntimeReason Reason, int Weight)> ReasonWeights)
{
    private static readonly MachineSimulationProfile Weaving = new(
        StopProbability: 0.08,          // %8 yarn-break risk per tick
        DefectRate: 0.03,
        MaintenanceProbability: 0.01,
        RpmMin: 600, RpmMax: 1200,
        BaseVibration: 1.5,
        WearPerTick: 0.08,
        ReasonWeights: new[] { (DowntimeReason.YarnBreak, 7), (DowntimeReason.OperatorWaiting, 2), (DowntimeReason.PowerFailure, 1) });

    private static readonly MachineSimulationProfile Dyeing = new(
        StopProbability: 0.05,          // %5 chemical-waiting risk
        DefectRate: 0.04,
        MaintenanceProbability: 0.015,
        RpmMin: 100, RpmMax: 300,
        BaseVibration: 1.0,
        WearPerTick: 0.05,
        ReasonWeights: new[] { (DowntimeReason.MaterialWaiting, 6), (DowntimeReason.PowerFailure, 1) });

    private static readonly MachineSimulationProfile Cutting = new(
        StopProbability: 0.03,          // %3 blade-change risk
        DefectRate: 0.02,
        MaintenanceProbability: 0.02,
        RpmMin: 300, RpmMax: 800,
        BaseVibration: 1.2,
        WearPerTick: 0.06,
        ReasonWeights: new[] { (DowntimeReason.Maintenance, 5), (DowntimeReason.OperatorWaiting, 2) });

    public static MachineSimulationProfile For(MachineType type) => type switch
    {
        MachineType.Weaving => Weaving,
        MachineType.Dyeing => Dyeing,
        MachineType.Cutting => Cutting,
        _ => Cutting
    };

    public DowntimeReason PickReason(Random rng)
    {
        var total = ReasonWeights.Sum(x => x.Weight);
        var roll = rng.Next(total);
        var acc = 0;
        foreach (var (reason, weight) in ReasonWeights)
        {
            acc += weight;
            if (roll < acc) return reason;
        }
        return ReasonWeights[0].Reason;
    }
}
