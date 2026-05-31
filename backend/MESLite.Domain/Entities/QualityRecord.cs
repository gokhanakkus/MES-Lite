using MESLite.Domain.Common;

namespace MESLite.Domain.Entities;

/// <summary>
/// Quality sample for a machine: produced vs. defective quantity. Feeds the OEE Quality factor.
/// </summary>
public class QualityRecord : BaseEntity
{
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public int ProducedQuantity { get; set; }

    public int DefectQuantity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Quality ratio (good / produced) in the 0..1 range.</summary>
    public double QualityRate => ProducedQuantity == 0 ? 0 : (double)(ProducedQuantity - DefectQuantity) / ProducedQuantity;
}
