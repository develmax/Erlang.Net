using System.Diagnostics;

namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private bool wallTimeEnabled;
    private long wallTimeGeneration;

    // Host-only legacy diagnostic mutation. Public native BIFs use queued changes.
    internal bool ExchangeWallTime(bool enabled)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(retiring, this);
            bool previous = wallTimeEnabled;
            if (previous != enabled && enabled) wallTimeGeneration++;
            wallTimeEnabled = enabled;
            foreach (RuntimeSchedulerLane lane in lanes) SetLaneWallTime(lane, enabled);

            return previous;
        }
    }

    private static void SetLaneWallTime(RuntimeSchedulerLane lane, bool enabled)
    {
        if (lane.WallTimeEnabled == enabled) return;
        long now = Stopwatch.GetTimestamp();
        if (enabled)
        {
            lane.WallTimeOrigin = now;
            lane.WallTimeActiveTicks = 0;
            lane.WallTimeActiveStarted = lane.HasRunningWork420 ? now : 0;
        }
        else if (lane.HasRunningWork420)
            lane.WallTimeActiveTicks += now - lane.WallTimeActiveStarted;
        lane.WallTimeEnabled = enabled;
    }

    private void WallTimeBecameActive(RuntimeSchedulerLane lane)
    {
        if (lane.WallTimeEnabled) lane.WallTimeActiveStarted = Stopwatch.GetTimestamp();
    }

    private void WallTimeBecameIdle(RuntimeSchedulerLane lane)
    {
        if (lane.WallTimeEnabled)
            lane.WallTimeActiveTicks += Stopwatch.GetTimestamp() - lane.WallTimeActiveStarted;
    }

    private static RuntimeManagedSchedulerWallTimeSample SnapshotLaneWallTime(RuntimeSchedulerLane lane)
    {
        long now = Stopwatch.GetTimestamp();

        return new(
            lane.Identity,
            lane.WallTimeActiveTicks + (lane.HasRunningWork420 ? now - lane.WallTimeActiveStarted : 0),
            now - lane.WallTimeOrigin,
            lane.WallTimeOrigin,
            lane.Online
        );
    }

    internal RuntimeManagedSchedulerWallTimeSnapshot WallTimeSnapshot()
    {
        lock (gate)
        {
            // A host diagnostic only reports a fully enabled aggregate. Partial
            // acknowledgment is represented by the BIF's per-request lane replies.
            bool completedEnabled = wallTimeEnabled && lanes.All(lane => lane.WallTimeEnabled);

            return new(
                completedEnabled,
                wallTimeGeneration,
                Stopwatch.Frequency,
                completedEnabled ? lanes.Select(SnapshotLaneWallTime) : []
            );
        }
    }
}
