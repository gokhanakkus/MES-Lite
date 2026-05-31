using MESLite.Domain.Entities;

namespace MESLite.Simulator;

/// <summary>
/// Physical telemetry model for a machine. Each running tick it evolves load, RPM, speed, wear,
/// vibration and temperature, then derives a Machine Health Score (0-100). These values feed back
/// into the simulation: low health lowers throughput and raises failure probability.
///
///   Wear        rises with load; reset by maintenance.
///   Vibration   = base + f(wear, load)         → high vibration ⇒ maintenance risk.
///   Temperature = ambient + f(load, wear)       → high temperature ⇒ failure risk.
///   Health      = 100 − wear penalty − vibration penalty − temperature penalty.
///   Speed       = idealRate × efficiency, where efficiency is dragged down by poor health.
/// </summary>
public static class MachineTelemetrySimulator
{
    private const double Ambient = 30.0;

    /// <summary>Advance telemetry for a running machine. Returns the produced-unit throughput (units/hour).</summary>
    public static void StepRunning(Machine m, MachineSimulationProfile profile, Random rng)
    {
        // Load random-walks within a realistic operating band.
        m.Load = Clamp(m.Load <= 0 ? rng.Next(45, 85) : m.Load + (rng.NextDouble() * 16 - 8), 30, 100);

        // Wear accumulates faster under higher load.
        m.WearLevel = Clamp(m.WearLevel + profile.WearPerTick * (0.5 + m.Load / 100.0), 0, 100);

        // RPM scales with load across the machine's nominal band (+ noise).
        var rpmSpan = profile.RpmMax - profile.RpmMin;
        m.Rpm = (int)Clamp(profile.RpmMin + rpmSpan * (m.Load / 100.0) + (rng.NextDouble() * 40 - 20), profile.RpmMin, profile.RpmMax);

        // Vibration grows with wear and load.
        m.Vibration = Clamp(profile.BaseVibration + m.WearLevel * 0.045 + (m.Load - 50) * 0.02 + (rng.NextDouble() * 0.6 - 0.3), 0, 14);

        // Temperature rises with load and wear above ambient.
        m.Temperature = Clamp(40 + m.Load * 0.30 + m.WearLevel * 0.12 + (rng.NextDouble() * 4 - 2), Ambient, 98);

        // Health derived from the three stressors (wear dominant).
        m.HealthScore = ComputeHealth(m);

        // Efficiency fluctuates 78-96% but is dragged down as health degrades.
        var healthFactor = 0.55 + 0.45 * (m.HealthScore / 100.0);
        m.Efficiency = Clamp((0.78 + rng.NextDouble() * 0.18) * healthFactor, 0.30, 1.0);

        // Speed (units/hour) and cycle time follow from efficiency.
        m.Speed = Math.Round(m.IdealRunRatePerHour * m.Efficiency, 1);
        m.CycleTimeSeconds = m.Speed > 0 ? Math.Round(3600.0 / m.Speed, 2) : 0;
    }

    /// <summary>Telemetry for a stopped / under-maintenance machine: spins down and cools toward ambient.</summary>
    public static void StepIdle(Machine m, Random rng)
    {
        m.Load = Clamp(m.Load - 25, 0, 100);
        m.Rpm = (int)Clamp(m.Rpm - 200, 0, m.Rpm);
        m.Speed = 0;
        m.CycleTimeSeconds = 0;
        m.Efficiency = 0;
        m.Vibration = Clamp(m.Vibration - 0.5, 0, 14);
        m.Temperature = Clamp(m.Temperature - 3 + (rng.NextDouble() - 0.5), Ambient, 98);
        m.HealthScore = ComputeHealth(m);
    }

    /// <summary>Restore condition after maintenance: most wear removed, stressors reset.</summary>
    public static void ApplyMaintenance(Machine m, MachineSimulationProfile profile)
    {
        m.WearLevel = Math.Round(m.WearLevel * 0.12, 1);
        m.Vibration = profile.BaseVibration;
        m.Temperature = 45;
        m.HealthScore = ComputeHealth(m);
    }

    /// <summary>
    /// Base stop probability amplified by the machine's condition. Worn, hot, heavily-vibrating
    /// machines stop far more often than healthy ones.
    /// </summary>
    public static double EffectiveStopProbability(Machine m, MachineSimulationProfile profile)
    {
        var risk = 1.0
                   + m.WearLevel / 100.0
                   + Math.Max(0, m.Vibration - 5) * 0.15
                   + Math.Max(0, m.Temperature - 80) * 0.05
                   + (100 - m.HealthScore) / 150.0;
        return Math.Min(0.9, profile.StopProbability * risk);
    }

    /// <summary>Maintenance probability, climbing steeply as health falls into the risk zone.</summary>
    public static double EffectiveMaintenanceProbability(Machine m, MachineSimulationProfile profile)
    {
        var risk = 1.0 + (100 - m.HealthScore) / 60.0;
        if (m.HealthScore < 30) risk += 1.5;
        return Math.Min(0.9, profile.MaintenanceProbability * risk);
    }

    private static double ComputeHealth(Machine m)
    {
        var health = 100.0
                     - m.WearLevel * 0.7
                     - Math.Max(0, m.Vibration - 4) * 4.0
                     - Math.Max(0, m.Temperature - 75) * 1.2;
        return Math.Round(Clamp(health, 0, 100), 1);
    }

    private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));
}
