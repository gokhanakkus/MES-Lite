namespace MESLite.Application.Common.Models;

/// <summary>Aggregation window for OEE and reports.</summary>
public enum PeriodType
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2
}

public static class PeriodTypeExtensions
{
    /// <summary>
    /// Resolve a [from, to) UTC window for the given period, as a rolling window ending at
    /// <paramref name="reference"/>. Rolling (last 24h / 7d / 30d) rather than calendar-aligned so
    /// OEE is always computed over a populated window — no "empty start of day" distortion.
    /// </summary>
    public static (DateTime From, DateTime To) ToRange(this PeriodType period, DateTime reference)
    {
        var to = reference;
        var from = period switch
        {
            PeriodType.Daily => reference.AddDays(-1),
            PeriodType.Weekly => reference.AddDays(-7),
            PeriodType.Monthly => reference.AddDays(-30),
            _ => reference.AddDays(-1)
        };
        return (from, to);
    }
}
