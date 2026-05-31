using MESLite.Domain.Common;

namespace MESLite.Domain.Entities;

/// <summary>
/// A single production tick: how much a machine produced at a point in time.
/// </summary>
public class ProductionRecord : BaseEntity
{
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public int? OperatorId { get; set; }
    public Operator? Operator { get; set; }

    /// <summary>Quantity produced in this tick (meters or pieces depending on machine type).</summary>
    public int Quantity { get; set; }

    public DateTime ProducedAt { get; set; } = DateTime.UtcNow;
}
