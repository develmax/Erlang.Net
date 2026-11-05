namespace Erlang;

internal sealed partial class DirtyNativeOwner419
{
    private bool schedulingPaused;
    private Task schedulingPausedDrain = Task.CompletedTask;

    internal Task SchedulingPauseCompletion
    {
        get
        {
            lock (gate)
            {
                return schedulingPausedDrain;
            }
        }
    }

    internal Task PauseScheduling()
    {
        lock (gate)
        {
            if (!schedulingPaused)
            {
                schedulingPaused = true;
                schedulingPausedDrain = Task.WhenAll(invocations.Where(call => call.Running).Select(call => call.Settled.Task));
            }

            return schedulingPausedDrain;
        }
    }

    internal void ResumeScheduling()
    {
        lock (gate)
        {
            if (!schedulingPaused)
            {
                return;
            }
            schedulingPaused = false;
            schedulingPausedDrain = Task.CompletedTask;
            // Workers may have consumed enqueue wakeups while parked. Restore
            // one credit per pending call; CPU and the shared IO queue differ.
            cpu.EnsureWakeupsLocked(cpu.Queue.Count);
            io.EnsureWakeupsLocked(io.Queue.Count);
        }
    }
}
