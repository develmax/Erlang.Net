using System.Diagnostics;

namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private bool wallTimeEnabled;
    private long wallTimeGeneration;

    internal bool ExchangeWallTime(bool enabled)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            bool previous = wallTimeEnabled;
            if (previous == enabled) return previous;
            long now = Stopwatch.GetTimestamp();
            if (enabled)
            {
                wallTimeGeneration++;
                foreach (var lane in lanes)
                {
                    lane.WallTimeOrigin = now;
                    lane.WallTimeActiveTicks = 0;
                    lane.WallTimeActiveStarted = lane.HasRunningWork420 ? now : 0;
                }
            }
            else
                foreach (var lane in lanes)
                    if (lane.HasRunningWork420)
                        lane.WallTimeActiveTicks += now - lane.WallTimeActiveStarted;
            // Every actual lane gate/timestamp is changed before return. No
            // actor continuation or ERTS physical-scheduler reply is fabricated.
            wallTimeEnabled = enabled;

            return previous;
        }
    }

    private void WallTimeBecameActive(RuntimeSchedulerLane lane)
    {
        if (wallTimeEnabled) lane.WallTimeActiveStarted = Stopwatch.GetTimestamp();
    }

    private void WallTimeBecameIdle(RuntimeSchedulerLane lane)
    {
        if (wallTimeEnabled)
            lane.WallTimeActiveTicks += Stopwatch.GetTimestamp() - lane.WallTimeActiveStarted;
    }

    internal RuntimeManagedSchedulerWallTimeSnapshot WallTimeSnapshot()
    {
        lock (gate)
        {
            if (!wallTimeEnabled)
                return new(
                    false,
                    wallTimeGeneration,
                    Stopwatch.Frequency,
                    []
                );
            long now = Stopwatch.GetTimestamp();

            return new(
                true,
                wallTimeGeneration,
                Stopwatch.Frequency,
                lanes.Select(lane =>
                new RuntimeManagedSchedulerWallTimeSample(
                    lane.Identity,
                    lane.WallTimeActiveTicks + (lane.HasRunningWork420 ? now - lane.WallTimeActiveStarted : 0),
                    now - lane.WallTimeOrigin,
                    lane.WallTimeOrigin,
                    lane.Online
                ))
            );
        }
    }
}
