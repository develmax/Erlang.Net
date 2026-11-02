using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimeDirtyWallTime
{
    private readonly object gate = new();
    private bool initialized;
    private bool active;
    private long origin;
    private long started;
    private long accumulated;

    internal void InitializeOnWorker()
    {
        lock (gate)
        {
            if (initialized) throw new InvalidOperationException(nameof(RuntimeDirtyWallTime));
            origin = Stopwatch.GetTimestamp();
            initialized = true;
        }
    }

    internal void BeginWork()
    {
        lock (gate)
        {
            if (!initialized || active) throw new InvalidOperationException(nameof(RuntimeDirtyWallTime));
            started = Stopwatch.GetTimestamp();
            active = true;
        }
    }

    internal void EndWork()
    {
        lock (gate)
        {
            if (!initialized || !active) throw new InvalidOperationException(nameof(RuntimeDirtyWallTime));
            accumulated = checked(accumulated + Stopwatch.GetTimestamp() - started);
            active = false;
        }
    }

    internal RuntimeDirtyWallTimeSample Sample()
    {
        lock (gate)
        {
            if (!initialized) throw new InvalidOperationException(nameof(RuntimeDirtyWallTime));
            long now = Stopwatch.GetTimestamp();
            long total = now - origin;
            long working = checked(accumulated + (active ? now - started : 0));

            return new(Math.Min(working, total), total);
        }
    }
}
