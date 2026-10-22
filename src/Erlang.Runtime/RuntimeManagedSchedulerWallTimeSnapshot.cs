namespace Erlang;

public sealed class RuntimeManagedSchedulerWallTimeSnapshot
{
    internal RuntimeManagedSchedulerWallTimeSnapshot(
        bool enabled,
        long generation,
        long frequency,
        IEnumerable<RuntimeManagedSchedulerWallTimeSample> lanes
    )
    {
        Enabled = enabled;
        Generation = generation;
        TimestampFrequency = frequency;
        Lanes = Array.AsReadOnly(lanes.ToArray());
    }

    public bool Enabled { get; }
    public long Generation { get; }
    public long TimestampFrequency { get; }
    public IReadOnlyList<RuntimeManagedSchedulerWallTimeSample> Lanes { get; }
}
