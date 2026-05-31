using MESLite.Domain.Common;
using MESLite.Domain.Enums;

namespace MESLite.Domain.Entities;

/// <summary>
/// A stoppage period for a machine. <see cref="EndTime"/> is null while the stop is ongoing.
/// </summary>
public class Downtime : BaseEntity
{
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public DowntimeReason Reason { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    /// <summary>True while the machine is still stopped (no end recorded yet).</summary>
    public bool IsOngoing => EndTime is null;

    /// <summary>Duration of the stop. Uses "now" while still ongoing.</summary>
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
}
