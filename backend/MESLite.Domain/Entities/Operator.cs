using MESLite.Domain.Common;
using MESLite.Domain.Enums;

namespace MESLite.Domain.Entities;

/// <summary>
/// A shop-floor operator. Linked to production records to drive operator performance.
/// </summary>
public class Operator : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public Shift Shift { get; set; }

    // Navigation
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
}
