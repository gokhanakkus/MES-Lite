namespace MESLite.Application.Common.Oee;

/// <summary>
/// Immutable OEE computation result. All ratios are in the 0..1 range.
/// OEE = Availability × Performance × Quality.
/// </summary>
public sealed record OeeResult
{
    public double Availability { get; init; }
    public double Performance { get; init; }
    public double Quality { get; init; }
    public double Oee => Availability * Performance * Quality;

    // Raw inputs kept for transparency / drill-down on the dashboard.
    public double PlannedMinutes { get; init; }
    public double DowntimeMinutes { get; init; }
    public double RunTimeMinutes => Math.Max(0, PlannedMinutes - DowntimeMinutes);
    public int TotalProduced { get; init; }
    public int TotalDefects { get; init; }

    public static OeeResult Empty => new();
}
