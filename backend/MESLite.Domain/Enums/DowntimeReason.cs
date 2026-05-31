namespace MESLite.Domain.Enums;

/// <summary>
/// Why a machine stopped. Drives downtime analytics and AI maintenance insights.
/// </summary>
public enum DowntimeReason
{
    /// <summary>İplik kopması — yarn break (weaving).</summary>
    YarnBreak = 0,

    /// <summary>Malzeme/kimyasal bekleme — waiting for material or chemical.</summary>
    MaterialWaiting = 1,

    /// <summary>Planlı/plansız bakım.</summary>
    Maintenance = 2,

    /// <summary>Operatör bekleniyor.</summary>
    OperatorWaiting = 3,

    /// <summary>Elektrik kesintisi.</summary>
    PowerFailure = 4
}
