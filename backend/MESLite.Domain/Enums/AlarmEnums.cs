namespace MESLite.Domain.Enums;

/// <summary>The telemetry metric an alarm is about.</summary>
public enum AlarmMetric
{
    Temperature = 0,
    Vibration = 1,
    Rpm = 2,
    Health = 3,
    Wear = 4
}

public enum AlarmSeverity
{
    Warning = 0,
    Critical = 1
}

/// <summary>How the anomaly was detected.</summary>
public enum AlarmDetector
{
    /// <summary>Fixed engineering limit exceeded.</summary>
    Threshold = 0,

    /// <summary>Statistical outlier vs. the machine's own recent baseline (z-score).</summary>
    Statistical = 1
}
