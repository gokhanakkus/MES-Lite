namespace MESLite.Application.Common.Oee;

/// <summary>
/// Pure OEE math, free of EF/DB concerns so it can be unit-tested in isolation.
///
/// Availability = RunTime / PlannedTime          (RunTime = Planned - Downtime)
/// Performance  = ActualProduced / IdealProduced (IdealProduced = idealRatePerHour × RunTimeHours)
/// Quality      = Good / Produced                (Good = Produced - Defects)
/// OEE          = Availability × Performance × Quality
///
/// Each factor is clamped to [0, 1] so noisy simulator data never yields impossible OEE.
/// </summary>
public static class OeeCalculator
{
    public static OeeResult Compute(
        double plannedMinutes,
        double downtimeMinutes,
        int totalProduced,
        int idealRatePerHour,
        int qualityProduced,
        int qualityDefects)
    {
        if (plannedMinutes <= 0)
        {
            return OeeResult.Empty with { PlannedMinutes = plannedMinutes };
        }

        var clampedDowntime = Math.Clamp(downtimeMinutes, 0, plannedMinutes);
        var runTimeMinutes = plannedMinutes - clampedDowntime;

        var availability = Clamp01(runTimeMinutes / plannedMinutes);

        double performance = 0;
        if (runTimeMinutes > 0 && idealRatePerHour > 0)
        {
            var idealProduced = idealRatePerHour * (runTimeMinutes / 60.0);
            performance = Clamp01(totalProduced / idealProduced);
        }

        double quality = 0;
        if (qualityProduced > 0)
        {
            quality = Clamp01((double)(qualityProduced - qualityDefects) / qualityProduced);
        }

        return new OeeResult
        {
            Availability = availability,
            Performance = performance,
            Quality = quality,
            PlannedMinutes = plannedMinutes,
            DowntimeMinutes = clampedDowntime,
            TotalProduced = totalProduced,
            TotalDefects = qualityDefects
        };
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);
}
