namespace Erlang;

internal sealed class RuntimeMultiSchedulingBlocker(ProcessRuntime runtime, ProcessContext context)
{
    internal ProcessContext Context { get; } = context;
    internal int AllCount { get; set; }
    internal int NormalCount { get; set; }
    // Intent suspends new dirty work; only a completed barrier commits the
    // corresponding blocker to public state and blocker-list observations.
    internal bool AllPending { get; set; }
    internal bool NormalPending { get; set; }
    internal bool HasAllBlock => AllCount > (AllPending ? 1 : 0);
    internal bool HasNormalBlock => NormalCount > (NormalPending ? 1 : 0);
    internal CancellationTokenRegistration Registration { get; set; }

    internal void OwnerStopped() => runtime.ReleaseMultiSchedulingBlocker(this);
}
