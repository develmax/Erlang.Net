namespace Erlang;

public sealed partial class ModuleRegistry
{
    private HashSet<ModuleCodeImage>? nifOwnedImages;
    private HashSet<NifImageLoadReservation>? nifReservations;
    private Dictionary<ModuleCodeImage, NifRetirementOperation>? nifRetirements;
    private System.Runtime.CompilerServices.ConditionalWeakTable<ModuleCodeImage, NifRetirementOperation>? nifCompletedRetirements;
    private List<Task>? nifRetirementFailures;
    private TaskCompletionSource? nifShutdown;
    private bool nifClosing;
    internal bool NifLifetimeClosing => Volatile.Read(ref nifClosing);

    private void ThrowIfNifClosing()
    {
        if (nifClosing)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.RuntimeClosing);
    }

    private void OwnNifReservation(NifImageLoadReservation reservation)
    {
        nifOwnedImages ??= new();
        nifReservations ??= new();
        bool added = nifOwnedImages.Add(reservation.Image);
        try
        {
            nifReservations.Add(reservation);
        }
        catch
        {
            if (added)
                nifOwnedImages.Remove(reservation.Image);
            throw;
        }
    }

    internal void NifReservationEnded(NifImageLoadReservation reservation)
    {
        lock (gate)
        {
            nifReservations?.Remove(reservation);
            if (reservation.Image.NifBindings.Phase == NifImageBindingPhase.Empty)
                nifOwnedImages?.Remove(reservation.Image);
        }
    }

    private Task QueueNifRetirement(ModuleCodeImage image)
    {
        if (!NifImageBindingStore.TryGetValue(image, out var bindings))
            return Task.CompletedTask;
        nifRetirements ??= new();
        if (nifRetirements.TryGetValue(image, out var pending))
            return pending.Completion;
        if (nifCompletedRetirements is not null && nifCompletedRetirements.TryGetValue(image, out var completed))
            return completed.Completion;
        nifRetirements.EnsureCapacity(checked(nifRetirements.Count + 1));
        var operation = new NifRetirementOperation(this, image, bindings!);
        // Close guest admission and unlink old-owned resource names before
        // logical purge returns. Do not wait for native calls or image unload.
        bindings!.CloseAdmission529();
        nifRetirements.Add(image, operation);
        operation.Start();

        return operation.Completion;
    }

    internal Task RetireNifImageAsync(ModuleCodeImage image)
    {
        lock (gate)
            return QueueNifRetirement(image);
    }

    internal void NifRetirementEnded(NifRetirementOperation operation, Exception? failure, bool completed)
    {
        lock (gate)
        {
            if (nifRetirements is not null && nifRetirements.TryGetValue(operation.Image, out var pending) && ReferenceEquals(pending, operation))
                nifRetirements.Remove(operation.Image);
            if (completed)
            {
                nifCompletedRetirements ??= new();
                nifCompletedRetirements.Add(operation.Image, operation);
                nifOwnedImages?.Remove(operation.Image);
            }
            if (failure is not null)
            {
                nifRetirementFailures ??= new();
                nifRetirementFailures.Add(operation.Completion);
            }
        }
    }

    internal void RequireNifShutdownCaller()
    {
        RequireSourceShutdownCaller();
        if (NifRetirementFlow.IsInside(this))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantRetirementOperation);
        if (codeModificationPermission is { } permission && NifImageInitializationFlow.HoldsPermission(permission))
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantInitializationOperation);
        if (Monitor.IsEntered(gate) && HasNifShutdownOwnership())
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantRegistryShutdown);
    }

    internal Task CloseNifLifetimeAsync()
    {
        RequireNifShutdownCaller();
        // Wake/fault Host entrants before any image/call joins can wait them.
        owningRuntime?.CloseNormalNativeAdmission697();
        TaskCompletionSource completion;
        NifImageLoadReservation[] reservations;
        Task[] retirements;
        lock (gate)
        {
            if (nifShutdown is not null)
                return nifShutdown.Task;
            nifClosing = true;
            nifShutdown = completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            reservations = nifReservations?.ToArray() ?? [];
            ModuleCodeImage[] images = (nifOwnedImages ?? Enumerable.Empty<ModuleCodeImage>())
                .Concat(codeImages.Values)
                .Concat(oldCode.Values.Select(old => old.Image))
                .Concat(SavedOldImages432())
                .Concat(pendingBeamImages.Values.Select(ticket => ticket.CodeImage))
                .Distinct().ToArray();
            var queued = images.Select(QueueNifRetirement).ToArray();
            retirements = queued
                .Concat(nifRetirements?.Values.Select(operation => operation.Completion) ?? [])
                .Concat(nifRetirementFailures ?? Enumerable.Empty<Task>()).Distinct().ToArray();
        }
        try
        {
            _ = Task.Run(NifImageInitializationFlow.IsolateActor(() => CompleteNifShutdown(reservations, retirements, completion)));
        }
        catch (Exception failure)
        {
            completion.TrySetException(failure);
        }

        return completion.Task;
    }

    private static async Task CompleteNifShutdown(
        NifImageLoadReservation[] reservations,
        Task[] retirements,
        TaskCompletionSource completion
    )
    {
        Task? all = null;
        try
        {
            var joins = reservations.Select(JoinNifReservationForShutdown).Concat(retirements).ToArray();
            all = Task.WhenAll(joins);
            await all.ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception failure)
        {
            completion.TrySetException(all?.Exception ?? failure);
        }
    }

    private static async Task JoinNifReservationForShutdown(NifImageLoadReservation reservation)
    {
        try
        {
            await reservation.DisposeAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (reservation.Generation.Context.Cancellation.IsCancellationRequested)
        {
        }
        catch (NifImageAdmissionException failure) when (failure.Failure is NifImageAdmissionFailure.ReservationAbandoned or NifImageAdmissionFailure.RuntimeClosing)
        {
        }
    }

    // Called only while this thread already owns gate; never acquire it merely
    // to choose the shutdown scheduling path.
    private bool HasNifShutdownOwnership()
        => nifOwnedImages is { Count: > 0 }
            || nifReservations is { Count: > 0 }
            || nifRetirements is { Count: > 0 }
            || nifRetirementFailures is { Count: > 0 }
            || codeImages.Values.Any(NifImageBindingStore.IsCreated)
            || oldCode.Values.Any(old => NifImageBindingStore.IsCreated(old.Image))
            || SavedOldImages432().Any(NifImageBindingStore.IsCreated)
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
