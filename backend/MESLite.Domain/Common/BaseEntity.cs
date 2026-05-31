namespace MESLite.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Keeps the identity strategy in one place.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
