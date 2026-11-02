namespace Erlang;

// Actual FIFO admission custody; entering native scope is a separate, synchronous
// operation on the physical calling thread. No callback scope crosses an await.
internal sealed class NifNativeLifecyclePermit616 : IDisposable
{
    private readonly CancellationTokenRegistration cancellation616;
    private readonly CancellationTokenRegistration actorExit616;
    internal readonly CancellationToken Cancellation616;
    internal NifActorAdmission616<NifNativeLifecyclePermit616>? Admission616;
    private readonly OperationCanceledException requestedExit616;
    private readonly OperationCanceledException? actorFailure616;
    internal readonly TaskCompletionSource Ready616 = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal readonly LinkedListNode<NifNativeLifecyclePermit616> Node616;
    internal NifNativeLifecyclePermitState616 State616;

    internal NifNativeLifecyclePermit616(CancellationToken cancellation, ProcessContext? actor)
    {
        Node616 = new(this);
        Cancellation616 = cancellation;
        requestedExit616 = new OperationCanceledException(cancellation);
        actorFailure616 = actor is null ? null : new OperationCanceledException(actor.Cancellation);
        cancellation616 = cancellation.UnsafeRegister(
            static state =>
        {
            var (permit, token) = ((NifNativeLifecyclePermit616, CancellationToken))state!;
            NifNativeLifecycleGate.Cancel616(permit, token);
            permit.Admission616?.RejectExternal616(permit.requestedExit616);
        },
            (this, cancellation)
        );
        try
        {
            if (actor is not null && actor.Cancellation != cancellation)
                actorExit616 = actor.Cancellation.UnsafeRegister(
                    static state =>
                {
                    var (permit, token) = ((NifNativeLifecyclePermit616, CancellationToken))state!;
                    NifNativeLifecycleGate.Cancel616(permit, token);
                    permit.Admission616?.RejectExternal616(permit.actorFailure616!);
                },
                    (this, actor.Cancellation)
                );
        }
        catch
        {
            cancellation616.Dispose();
            throw;
        }
    }

    internal NifNativeLifecyclePermit616 Claim616() => NifNativeLifecycleGate.Claim616(this);

    internal Scope Enter616()
    {
        NifNativeLifecycleGate.Enter616(this);

        return new Scope(this, Environment.CurrentManagedThreadId);
    }

    internal void AbandonUnclaimed616()
    {
        if (NifNativeLifecycleGate.AbandonUnclaimed616(this))
        {
            try
            {
                cancellation616.Dispose();
            }
            finally
            {
                actorExit616.Dispose();
            }
        }
    }

    public void Dispose()
    {
        NifNativeLifecycleGate.Release616(this);
        // Never wait for a cancellation callback while holding the queue lock.
        try
        {
            cancellation616.Dispose();
        }
        finally
        {
            actorExit616.Dispose();
        }
    }

    internal readonly struct Scope(NifNativeLifecyclePermit616 owner, int thread) : IDisposable
    {
        public void Dispose() => NifNativeLifecycleGate.Exit616(owner, thread);
    }
}
