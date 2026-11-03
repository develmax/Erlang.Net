using System.Runtime.Loader;

namespace Erlang;

internal static class NifNativeLifecycleGate
{
    private static readonly object gate = new();
    private static readonly LinkedList<NifNativeLifecyclePermit616> waiting616 = new();
    private static NifNativeLifecyclePermit616? active616;
    [ThreadStatic]
    private static bool entered;

    // Read-only real queue state, never an admission authority.
    internal static int WaitingAdmissions616
    {
        get
        {
            lock (gate) return waiting616.Count;
        }
    }

    internal static void RequireDefaultProvider()
    {
        if (!ReferenceEquals(AssemblyLoadContext.GetLoadContext(typeof(NifNativeLifecycleGate).Assembly), AssemblyLoadContext.Default))
            throw new NotSupportedException(NifNativeLifecycleNames.ProviderContext);
    }

    internal static NifActorAdmission616<NifNativeLifecyclePermit616> AcquireAsync616(CancellationToken cancellation)
        => Acquire616(cancellation, RuntimeActorExecutionTurn.CurrentNifAdmission616);

    internal static NifActorAdmission616<NifNativeLifecyclePermit616> AcquireHostAsync616()
        => Acquire616(CancellationToken.None, null);

    private static NifActorAdmission616<NifNativeLifecyclePermit616> Acquire616(
        CancellationToken cancellation,
        ProcessContext? actor
    )
    {
        RequireDefaultProvider();
        if (entered)
            throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
        cancellation.ThrowIfCancellationRequested();
        // All ticket/continuation allocation precedes any real grant.
        var permit = new NifNativeLifecyclePermit616(cancellation, actor);
        NifActorAdmission616<NifNativeLifecyclePermit616> admission;
        try
        {
            admission = new(
                permit.Ready616.Task,
                actor,
                permit.Claim616,
                _ => permit.AbandonUnclaimed616()
            );
            permit.Admission616 = admission;
            lock (gate)
            {
                if (permit.State616 != NifNativeLifecyclePermitState616.Released)
                {
                    if (active616 is null)
                        Grant616(permit);
                    else
                    {
                        permit.State616 = NifNativeLifecyclePermitState616.Queued;
                        waiting616.AddLast(permit.Node616);
                    }
                }
            }
        }
        catch
        {
            permit.Dispose();
            throw;
        }

        return admission;
    }

    internal static NifNativeLifecyclePermit616 Claim616(NifNativeLifecyclePermit616 permit)
    {
        lock (gate)
        {
            if (!ReferenceEquals(active616, permit) || permit.State616 != NifNativeLifecyclePermitState616.Unclaimed)
                throw new OperationCanceledException(permit.Cancellation616);
            permit.State616 = NifNativeLifecyclePermitState616.Granted;

            return permit;
        }
    }

    private static void Grant616(NifNativeLifecyclePermit616 permit)
    {
        active616 = permit;
        permit.State616 = NifNativeLifecyclePermitState616.Unclaimed;
        permit.Ready616.TrySetResult();
    }

    internal static void Cancel616(NifNativeLifecyclePermit616 permit, CancellationToken cancellation)
    {
        lock (gate)
        {
            if (permit.State616 is not (NifNativeLifecyclePermitState616.Created
                or NifNativeLifecyclePermitState616.Queued or NifNativeLifecyclePermitState616.Unclaimed))
                return; // Claimed custody is never revoked under native code.
            permit.Ready616.TrySetCanceled(cancellation);
            ReleaseUnderGate616(permit);
        }
    }

    internal static void Enter616(NifNativeLifecyclePermit616 permit)
    {
        if (entered)
            throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
        lock (gate)
        {
            if (!ReferenceEquals(active616, permit) || permit.State616 != NifNativeLifecyclePermitState616.Granted)
                throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
            permit.State616 = NifNativeLifecyclePermitState616.Entered;
            entered = true;
        }
    }

    internal static void Exit616(NifNativeLifecyclePermit616 permit, int thread)
    {
        if (thread != Environment.CurrentManagedThreadId || !entered)
            throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
        lock (gate)
        {
            if (!ReferenceEquals(active616, permit) || permit.State616 != NifNativeLifecyclePermitState616.Entered)
                throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
            permit.State616 = NifNativeLifecyclePermitState616.Granted;
            entered = false;
        }
    }

    internal static bool AbandonUnclaimed616(NifNativeLifecyclePermit616 permit)
    {
        lock (gate)
        {
            if (permit.State616 == NifNativeLifecyclePermitState616.Released) return true;
            if (permit.State616 is not (NifNativeLifecyclePermitState616.Created
                or NifNativeLifecyclePermitState616.Queued or NifNativeLifecyclePermitState616.Unclaimed))
                return false;
            ReleaseUnderGate616(permit);

            return true;
        }
    }

    internal static void Release616(NifNativeLifecyclePermit616 permit)
    {
        lock (gate) ReleaseUnderGate616(permit);
    }

    private static void ReleaseUnderGate616(NifNativeLifecyclePermit616 permit)
    {
        if (permit.State616 == NifNativeLifecyclePermitState616.Entered)
            throw new InvalidOperationException(NifNativeLifecycleNames.ReentrantOperation);
        if (permit.State616 == NifNativeLifecyclePermitState616.Released) return;
        if (permit.Node616.List is not null) waiting616.Remove(permit.Node616);
        permit.State616 = NifNativeLifecyclePermitState616.Released;
        permit.Ready616.TrySetCanceled();
        if (!ReferenceEquals(active616, permit)) return;
        active616 = null;
        if (waiting616.First is { } next)
        {
            waiting616.RemoveFirst();
            Grant616(next.Value);
        }
    }
}
