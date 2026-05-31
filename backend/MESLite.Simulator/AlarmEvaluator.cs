using MESLite.Domain.Entities;
using MESLite.Domain.Enums;

namespace MESLite.Simulator;

/// <summary>A currently-triggered alarm condition for a machine/metric.</summary>
public sealed record AlarmCondition(
    AlarmMetric Metric,
    AlarmSeverity Severity,
    AlarmDetector Detector,
    string Message,
    double Value,
    double Limit);

/// <summary>
/// Rule-based anomaly detector combining two strategies:
///   1. <b>Threshold</b> — fixed engineering limits (temperature, vibration, wear, health).
///   2. <b>Statistical</b> — per-machine z-score vs. its own recent baseline (temperature, vibration,
///      RPM). Detects deviations relative to "normal for this machine", learned from history.
///
/// Holds rolling baselines in memory (warms up after a handful of samples). Returns the worst
/// condition per metric so the caller can drive the raised/resolved alarm lifecycle.
/// </summary>
public sealed class AlarmEvaluator
{
    private const int BaselineWindow = 120;
    private const int MinSamples = 20;

    // Static engineering limits, calibrated to the simulator's physical operating ranges.
    private const double TempWarn = 76, TempCrit = 84;
    private const double VibWarn = 5.0, VibCrit = 6.8;
    private const double WearWarn = 60, WearCrit = 82;
    private const double HealthWarn = 40, HealthCrit = 25;

    private readonly Dictionary<(int MachineId, AlarmMetric Metric), List<double>> _baselines = new();

    public IReadOnlyList<AlarmCondition> Evaluate(Machine m, bool running)
    {
        var conditions = new List<AlarmCondition>();

        AddWorst(conditions, AlarmMetric.Temperature,
            HighThreshold(AlarmMetric.Temperature, m.Temperature, TempWarn, TempCrit, "°C", "Sıcaklık"),
            running ? Statistical(m.Id, AlarmMetric.Temperature, m.Temperature, "°C", "Sıcaklık") : null);

        AddWorst(conditions, AlarmMetric.Vibration,
            HighThreshold(AlarmMetric.Vibration, m.Vibration, VibWarn, VibCrit, "mm/s", "Titreşim"),
            running ? Statistical(m.Id, AlarmMetric.Vibration, m.Vibration, "mm/s", "Titreşim") : null);

        AddWorst(conditions, AlarmMetric.Rpm,
            null,
            running ? Statistical(m.Id, AlarmMetric.Rpm, m.Rpm, "RPM", "Devir") : null);

        var wear = HighThreshold(AlarmMetric.Wear, m.WearLevel, WearWarn, WearCrit, "%", "Aşınma");
        if (wear is not null) conditions.Add(wear);

        var health = LowThreshold(m.HealthScore);
        if (health is not null) conditions.Add(health);

        // Update baselines (after evaluation) for the statistical metrics while running.
        if (running)
        {
            Push(m.Id, AlarmMetric.Temperature, m.Temperature);
            Push(m.Id, AlarmMetric.Vibration, m.Vibration);
            Push(m.Id, AlarmMetric.Rpm, m.Rpm);
        }

        return conditions;
    }

    private static void AddWorst(List<AlarmCondition> list, AlarmMetric _, AlarmCondition? a, AlarmCondition? b)
    {
        var pick = (a, b) switch
        {
            (null, null) => (AlarmCondition?)null,
            (not null, null) => a,
            (null, not null) => b,
            _ => a!.Severity >= b!.Severity ? a : b
        };
        if (pick is not null) list.Add(pick);
    }

    private static AlarmCondition? HighThreshold(AlarmMetric metric, double value, double warn, double crit, string unit, string label)
    {
        if (value >= crit)
            return new AlarmCondition(metric, AlarmSeverity.Critical, AlarmDetector.Threshold,
                $"{label} kritik eşiği aştı: {value:0.#}{unit} (limit {crit:0.#}{unit})", value, crit);
        if (value >= warn)
            return new AlarmCondition(metric, AlarmSeverity.Warning, AlarmDetector.Threshold,
                $"{label} uyarı eşiğini aştı: {value:0.#}{unit} (limit {warn:0.#}{unit})", value, warn);
        return null;
    }

    private static AlarmCondition? LowThreshold(double health)
    {
        if (health <= HealthCrit)
            return new AlarmCondition(AlarmMetric.Health, AlarmSeverity.Critical, AlarmDetector.Threshold,
                $"Sağlık skoru kritik seviyede: {health:0.#} (limit {HealthCrit:0.#})", health, HealthCrit);
        if (health <= HealthWarn)
            return new AlarmCondition(AlarmMetric.Health, AlarmSeverity.Warning, AlarmDetector.Threshold,
                $"Sağlık skoru düşük: {health:0.#} (limit {HealthWarn:0.#})", health, HealthWarn);
        return null;
    }

    private AlarmCondition? Statistical(int machineId, AlarmMetric metric, double value, string unit, string label)
    {
        var buf = _baselines.GetValueOrDefault((machineId, metric));
        if (buf is null || buf.Count < MinSamples) return null;

        var mean = buf.Average();
        var variance = buf.Sum(x => (x - mean) * (x - mean)) / buf.Count;
        var std = Math.Sqrt(variance);
        if (std < 0.3) return null; // too stable to judge; avoids divide-by-near-zero false positives

        var z = (value - mean) / std;
        if (z < 3) return null; // only flag high-side deviations

        var severity = z >= 4.5 ? AlarmSeverity.Critical : AlarmSeverity.Warning;
        return new AlarmCondition(metric, severity, AlarmDetector.Statistical,
            $"{label} geçmiş ortalamanın üzerinde: {value:0.#}{unit} (ort {mean:0.#} ± {std:0.#}, z={z:0.#})",
            value, Math.Round(mean + 3 * std, 1));
    }

    private void Push(int machineId, AlarmMetric metric, double value)
    {
        var buf = _baselines.TryGetValue((machineId, metric), out var existing) ? existing : _baselines[(machineId, metric)] = new List<double>(BaselineWindow + 1);
        buf.Add(value);
        if (buf.Count > BaselineWindow) buf.RemoveAt(0);
    }
}
