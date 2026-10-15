namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Called only while this thread already owns gate; never acquire it merely
    // to choose the shutdown scheduling path.
    private bool HasNifShutdownOwnership()
        => nifOwnedImages is { Count: > 0 }
            || nifReservations is { Count: > 0 }
            || nifRetirements is { Count: > 0 }
            || nifRetirementFailures is { Count: > 0 }
            || codeImages.Values.Any(NifImageBindingStore.IsCreated)
            || oldCode.Values.Any(old => NifImageBindingStore.IsCreated(old.Image))
            || pendingBeamImages.Values.Any(ticket => NifImageBindingStore.IsCreated(ticket.CodeImage));

    internal Task StartOwnedNifShutdown()
    {
        RequireNifShutdownCaller();
        // A dormant reentrant caller must not queue a worker that would wait
        // for the monitor that caller retains while joining disposal.
        if (Monitor.IsEntered(gate))
            return CloseNifLifetimeAsync();

        return Task.Run(NifImageInitializationFlow.IsolateActor(
            NifRetirementFlow.IsolateActor(CloseNifLifetimeAsync)
        ));
    }
}
