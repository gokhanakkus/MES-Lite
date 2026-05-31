using MESLite.Domain.Common;
using MESLite.Domain.Enums;

namespace MESLite.Domain.Entities;

/// <summary>
/// A production machine on the shop floor (weaving / dyeing / cutting).
/// </summary>
public class Machine : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public MachineType MachineType { get; set; }

    public MachineStatus Status { get; set; } = MachineStatus.Stopped;

    public string ProductionLine { get; set; } = string.Empty;

    /// <summary>
    /// Ideal (nameplate) throughput per hour. Unit depends on machine type:
    /// meters for weaving/dyeing, pieces for cutting. Used as the OEE Performance baseline.
    /// </summary>
    public int IdealRunRatePerHour { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Live telemetry (physical state, updated by the simulator each tick) ---

    /// <summary>Overall Machine Health Score, 0-100. 100=new, ~60=attention, ~30=failure risk, 0=down.</summary>
    public double HealthScore { get; set; } = 100;

    /// <summary>Cumulative wear, 0-100. Rises with production/load, reset by maintenance.</summary>
    public double WearLevel { get; set; }

    /// <summary>Current rotational speed (revolutions per minute).</summary>
    public int Rpm { get; set; }

    /// <summary>Current throughput (units/hour: meters or pieces).</summary>
    public double Speed { get; set; }

    /// <summary>Mechanical load, 0-100 (%).</summary>
    public double Load { get; set; }

    /// <summary>Vibration (mm/s RMS). High values raise maintenance risk.</summary>
    public double Vibration { get; set; }

    /// <summary>Temperature (°C). High values raise failure probability.</summary>
    public double Temperature { get; set; } = 45;

    /// <summary>Instantaneous efficiency, 0-1.</summary>
    public double Efficiency { get; set; } = 0.9;

    /// <summary>Time to produce one unit (seconds).</summary>
    public double CycleTimeSeconds { get; set; }

    // Navigation
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
    public ICollection<Downtime> Downtimes { get; set; } = new List<Downtime>();
    public ICollection<QualityRecord> QualityRecords { get; set; } = new List<QualityRecord>();
}
