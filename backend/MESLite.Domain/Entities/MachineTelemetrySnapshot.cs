using MESLite.Domain.Common;

namespace MESLite.Domain.Entities;

/// <summary>
/// A point-in-time sensor reading for a machine. Stored as a time series so the UI can draw
/// vibration / temperature trends and the predictive engine can estimate Remaining Useful Life.
/// </summary>
public class MachineTelemetrySnapshot : BaseEntity
{
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public double HealthScore { get; set; }
    public double WearLevel { get; set; }
    public int Rpm { get; set; }
    public double Speed { get; set; }
    public double Load { get; set; }
    public double Vibration { get; set; }
    public double Temperature { get; set; }
    public double Efficiency { get; set; }
}
