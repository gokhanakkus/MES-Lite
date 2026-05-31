namespace MESLite.Domain.Enums;

/// <summary>
/// Operator working shift.
/// </summary>
public enum Shift
{
    /// <summary>Sabah — 08:00-16:00.</summary>
    Morning = 0,

    /// <summary>Akşam — 16:00-00:00.</summary>
    Evening = 1,

    /// <summary>Gece — 00:00-08:00.</summary>
    Night = 2
}
