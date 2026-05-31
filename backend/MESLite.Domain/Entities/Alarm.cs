using MESLite.Domain.Common;
using MESLite.Domain.Enums;

namespace MESLite.Domain.Entities;

/// <summary>
/// An anomaly/alarm raised for a machine when a telemetry metric breaches an engineering limit
/// or deviates statistically from its own baseline. Has a raised → resolved lifecycle.
/// </summary>
public class Alarm : BaseEntity
{
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public AlarmMetric Metric { get; set; }
    public AlarmSeverity Severity { get; set; }
    public AlarmDetector Detector { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>The metric value that triggered the alarm.</summary>
    public double Value { get; set; }

    /// <summary>The breached limit (threshold detector) or baseline reference (statistical detector).</summary>
    public double Limit { get; set; }

    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public bool IsActive => ResolvedAt is null;
}
