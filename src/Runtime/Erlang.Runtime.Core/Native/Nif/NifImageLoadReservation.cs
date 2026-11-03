using System.Runtime.ExceptionServices;

namespace Erlang;

internal sealed class NifImageLoadReservation : IAsyncDisposable
{
    private readonly ModuleRegistry registry;
    private readonly ModuleCodeModificationTicket permission;
    private readonly SourceCodeLease sourceLease;
    private readonly NifImageBindingReservation bindingReservation;
    private readonly TaskCompletionSource<Task> operation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int state;

    internal NifImageLoadReservation(
        ModuleRegistry registry,
        ModuleCodeModificationTicket permission,
        SourceCodeLease sourceLease,
        NifImageBindingReservation bindingReservation,
        NifImageAdmissionGeneration generation
    )
    {
        this.registry = registry;
        this.permission = permission;
        this.sourceLease = sourceLease;
        this.bindingReservation = bindingReservation;
        Generation = generation;
    }

    internal NifImageAdmissionGeneration Generation
    {
        get;
    }
    internal NifImageBindingReservation ResourceBindingReservation527 => bindingReservation;
    internal ModuleCodeImage Image => Generation.Caller.Image;
    internal NifImageAdmissionOrigin Origin => Generation.Origin;

    internal bool HoldsPermission(ModuleCodeModificationPermission owner) => permission.IsActiveFor(owner);

    internal bool DriverIsRunning => Volatile.Read(ref state) != NifImageAdmissionStates.Closed;

    internal Task InitializeAndCommitAsync(Func<CancellationToken, ValueTask<NifImageInitialized>> initializer)
    {
        ArgumentNullException.ThrowIfNull(initializer);
        if (Interlocked.CompareExchange(ref state, NifImageAdmissionStates.Initializing, NifImageAdmissionStates.Reserved) != NifImageAdmissionStates.Reserved)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReservationAbandoned);
        Task driver = RunInitialization(initializer);
        operation.TrySetResult(driver);

        return driver;
    }

    private async Task RunInitialization(Func<CancellationToken, ValueTask<NifImageInitialized>> initializer)
    {
        using var flow = NifImageInitializationFlow.Enter(this);
        NifImageInitialized? initialized = null;
        Exception? failure = null;
        bool published = false;
        try
        {
            Generation.Context.Runtime.ThrowIfHalted();
            Generation.Context.Cancellation.ThrowIfCancellationRequested();
            initialized = await initializer(Generation.Context.Cancellation).ConfigureAwait(false);
            ArgumentNullException.ThrowIfNull(initialized);
            ArgumentNullException.ThrowIfNull(initialized.Owner);
            ArgumentNullException.ThrowIfNull(initialized.Bindings);
            // Enumerating initializer-provided collections may execute caller code.
            // Materialize outside registry and permission ticket monitor scopes.
            var bindings = initialized.Bindings.ToArray();
            Generation.Context.Cancellation.ThrowIfCancellationRequested();
            permission.Run(() =>
            {
                registry.CommitNifImage(
                    this,
                    bindingReservation,
                    bindings,
                    initialized.Owner
                );

                return true;
            });
            published = true;
        }
        catch (Exception error)
        {
            failure = error;
        }
        if (!published && initialized?.Owner is { } owner)
        {
            try
            {
                await owner.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                failure = failure is null ? cleanupFailure : new AggregateException(failure, cleanupFailure);
            }
        }
        try
        {
            CloseAdmission();
        }
        catch (Exception closeFailure)
        {
            failure = failure is null ? closeFailure : new AggregateException(failure, closeFailure);
        }
        finally
        {
            Interlocked.Exchange(ref state, NifImageAdmissionStates.Closed);
        }
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal void BeginCommit()
    {
        if (Interlocked.CompareExchange(ref state, NifImageAdmissionStates.Committing, NifImageAdmissionStates.Initializing) != NifImageAdmissionStates.Initializing)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReservationAbandoned);
    }

    private void CloseAdmission()
    {
        try
        {
            bindingReservation.Dispose();
        }
        finally
        {
            try
            {
                sourceLease.Dispose();
            }
            finally
            {
                try
                {
                    permission.Dispose();
                }
                finally
                {
                    registry.NifReservationEnded(this);
                }
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        if (NifImageInitializationFlow.IsInside(this))
        {
            Interlocked.CompareExchange(ref state, NifImageAdmissionStates.AbandonRequested, NifImageAdmissionStates.Initializing);
            throw new NifImageAdmissionException(NifImageAdmissionFailure.ReentrantInitializationOperation);
        }
        if (Interlocked.CompareExchange(ref state, NifImageAdmissionStates.Closed, NifImageAdmissionStates.Reserved) == NifImageAdmissionStates.Reserved)
        {
            Task completion = Task.CompletedTask;
            try
            {
                CloseAdmission();
            }
            catch (Exception failure)
            {
                completion = Task.FromException(failure);
            }
            operation.TrySetResult(completion);

            return new ValueTask(completion);
        }
        Interlocked.CompareExchange(ref state, NifImageAdmissionStates.AbandonRequested, NifImageAdmissionStates.Initializing);

        return operation.Task.IsCompleted ? new ValueTask(operation.Task.Result) : new ValueTask(JoinOperation());
    }

    private async Task JoinOperation()
    {
        Task driver = await operation.Task.ConfigureAwait(false);
        await driver.ConfigureAwait(false);
    }
}
