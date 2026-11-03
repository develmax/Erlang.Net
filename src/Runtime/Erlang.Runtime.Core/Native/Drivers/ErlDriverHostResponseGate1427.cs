namespace Erlang;

// Internal one-shot host-owned test gate, bound to exactly one parent peer.
// No callback, frame mutation, authority/session output, or synthetic result.
internal sealed class ErlDriverHostResponseGate1427 : IDisposable
{
    private readonly object gate = new();
    private readonly ErlDriverHostOperation420 selected;
    private readonly bool armed;
    private readonly TaskCompletionSource<ErlDriverHostResponseWitness1427> reached = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ErlDriverHostPeer420? owner;
    private bool held;
    private bool disposed;

    internal ErlDriverHostResponseGate1427(ErlDriverHostOperation420 selected, bool armed = true)
    {
        if (selected is not (ErlDriverHostOperation420.AsyncAccountingCapture1151
            or ErlDriverHostOperation420.AsyncAccountingApply1151
            or ErlDriverHostOperation420.AsyncAccountingFinish1151
            or ErlDriverHostOperation420.AsyncAccountingRelease1151))
            throw new ArgumentOutOfRangeException(nameof(selected));
        this.selected = selected;
        this.armed = armed;
    }

    internal Task<ErlDriverHostResponseWitness1427> Reached => reached.Task;

    internal void Bind(ErlDriverHostPeer420 peer)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (owner is not null) throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            owner = peer;
        }
    }

    internal async ValueTask HoldAsync(
        ErlDriverHostPeer420 peer,
        ErlDriverHostResponseWitness1427 witness,
        CancellationToken stopped
    )
    {
        lock (gate)
        {
            if (!ReferenceEquals(owner, peer)) throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            if (disposed || !armed || held || release.Task.IsCompleted || witness.Operation != selected) return;
            if (witness.Request == 0 || witness.PendingCompletion.IsCompleted)
                throw new InvalidOperationException(ErlDriverHostNames420.InvalidFrame);
            held = true;
            reached.TrySetResult(witness);
        }
        await release.Task.WaitAsync(stopped).ConfigureAwait(false);
    }

    internal void Release() => release.TrySetResult();

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            release.TrySetResult();
            if (!held)
            {
                reached.TrySetException(new ObjectDisposedException(nameof(ErlDriverHostResponseGate1427)));
                _ = reached.Task.Exception;
            }
        }
    }
}
