namespace MESLite.Simulator;

/// <summary>Configurable knobs for the production simulator (bound from "Simulator" config section).</summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>Master switch. When false the background service idles.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often a simulation tick runs.</summary>
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Production speed multiplier. 1.0 = real-time (output rate ≈ ideal × efficiency, so OEE stays
    /// realistic). Values &gt; 1 fast-forward output but will inflate the OEE Performance factor.
    /// </summary>
    public double SpeedFactor { get; set; } = 1.0;

    /// <summary>Push an aggregated OEE update over SignalR every N ticks.</summary>
    public int OeeBroadcastEveryTicks { get; set; } = 6;
}
