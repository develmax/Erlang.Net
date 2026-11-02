namespace Erlang;

internal sealed partial class NifImageBindings
{
    private NativeResourceTypeTable527? resourceNames529;
    private NifImageRetirementState529? retirementState529;

    internal void BindResourceNames529(NativeResourceTypeTable527 table, NifImageBindingReservation authority)
    {
        lock (gate)
        {
            if (phase != NifImageBindingPhase.Initializing || !ReferenceEquals(reservation, authority)
                || resourceNames529 is not null && !ReferenceEquals(resourceNames529, table))
                throw new InvalidOperationException(NativeResource527Names.LoadPhase);
            resourceNames529 = table;
        }
    }

    // Called synchronously before the physical retirement worker is queued.
    // Neither active native calls nor a destructor/DSO close is awaited here.
    internal void CloseAdmission529()
    {
        while (true)
        {
            NativeResourceTypeTable527? table;
            lock (gate)
            {
                if (retirement is not null) return;
                if (CloseNativeTransition544()) return;
                closePreparation544 = true;
                table = resourceNames529;
            }
            using NativeResourceImageNamesRetirement529? names = table?.PrepareImageNamesRetirement529(Image);
            lock (gate)
            {
                if (retirement is not null) return;
                if (!ReferenceEquals(table, resourceNames529)) continue;
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource? pendingCalls = activeCalls == 0 ? null
                    : new(TaskCreationOptions.RunContinuationsAsynchronously);
                Task drain = pendingCalls?.Task ?? Task.CompletedTask;
                if (reservation is { } initializing)
                    drain = Task.WhenAll(drain, initializing.Settled);
                var state = new NifImageRetirementState529(
                    nativeOwner,
                    drain,
                    completion,
                    names
                );
                // All allocations precede the observable name/admission change.
                // The name writer precedes this binding monitor. Its owner-image
                // snapshot cannot change until this scope releases that writer.
                names?.Commit529();
                callsDrained = pendingCalls;
                retirementState529 = state;
                retirement = completion;
                phase = NifImageBindingPhase.Retired;
                ready = null;
                installedFunctions = Nil.Value;
                nativeOwner = null;

                return;
            }
        }
    }
}
