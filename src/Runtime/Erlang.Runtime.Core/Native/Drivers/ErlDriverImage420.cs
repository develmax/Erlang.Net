using System.Runtime.InteropServices;

namespace Erlang;

// Child-private real image owner. It cannot be constructed without a complete
// admitted callback table. No NIF entry/table is involved.
internal sealed class ErlDriverImage420
{
    private readonly object gate = new();
    private readonly ErlDriverCallbackTable420 callbacks;
    private readonly Func<ErlDriverImage420, IDisposable> enterOwner;
    private readonly TaskCompletionSource<bool> retire = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource workersExited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task retirement;
    private nint image;
    private int active;
    private int workers;
    private bool workerAdmissionClosed;
    private bool retiring;
    private bool tableRetained;
    private bool initialized;

    private ErlDriverImage420(ErlDriverCallbackTable420 callbacks, Func<ErlDriverImage420, IDisposable> enterOwner)
    {
        this.callbacks = callbacks;
        this.enterOwner = enterOwner;
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            retirement = RetireCore();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal ErlDriverEntry420 Entry { get; private set; } = null!;
    internal object DriverGate { get; } = new();

    internal IDisposable EnterOwner() => enterOwner(this);

    internal static async ValueTask<(ErlDriverImage420? Owner, int Error)> LoadAsync(
        ErlDriverCallbackTable420 callbacks,
        ErlDriverAbiLayout420 layout,
        string unextendedPath,
        int nativePathByteCount,
        ReadOnlyMemory<byte> name,
        Func<ErlDriverImage420, IDisposable> enterOwner,
        bool schedulerThread,
        Func<ErlDriverLinkedInitialization435> beginInitialization435
    )
    {
        var owner = new ErlDriverImage420(callbacks, enterOwner);
        try
        {
            callbacks.DangerousAddRef(ref owner.tableRetained);
            var loaded = ErlDriverWindowsLoader420.Open(unextendedPath, nativePathByteCount);
            owner.image = loaded.Image;
            if (loaded.Error != ErlDriverLoadErrors420.NoError) return await owner.Refuse(loaded.Error).ConfigureAwait(false);
            nint initialize = ErlDriverWindowsLoader420.InitializeSymbol(owner.image);
            if (initialize == nint.Zero) return await owner.Refuse(ErlDriverLoadErrors420.NoInitialize).ConfigureAwait(false);
            var call = Marshal.GetDelegateForFunctionPointer<ErlDriverEntryTypes420.DriverInitialize>(initialize);
            int error;
            using (enterOwner(owner))
            using (new ErlDriverThreadRole420(schedulerThread))
                error = owner.Initialize(
                    call,
                    layout,
                    name,
                    beginInitialization435
                );
            if (error != ErlDriverLoadErrors420.NoError) return await owner.Refuse(error).ConfigureAwait(false);

            return (owner, ErlDriverLoadErrors420.NoError);
        }
        catch (Exception error)
        {
            try
            {
                await owner.Refuse(ErlDriverLoadErrors420.FailedInitialize).ConfigureAwait(false);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(error, cleanup);
            }
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
            throw;
        }
    }

    private int Initialize(
        ErlDriverEntryTypes420.DriverInitialize initialize,
        ErlDriverAbiLayout420 layout,
        ReadOnlyMemory<byte> name,
        Func<ErlDriverLinkedInitialization435> beginInitialization435
    )
    {
        nint entry = initialize(callbacks.DangerousGetHandle());
        if (entry == nint.Zero) return ErlDriverLoadErrors420.FailedInitialize;
        Entry = new(entry, layout);
        if (!Entry.HasValidVersion) return ErlDriverLoadErrors420.IncorrectVersion;
        if (!Entry.NameEquals(name.Span)) return ErlDriverLoadErrors420.BadName;
        var init = Entry.Callback<ErlDriverEntryTypes420.Initialize>(ErlDriverEntryNames420.Init);
        if (init is not null)
        {
            using var linkedInitialization435 = beginInitialization435();
            int initializedResult;
            try
            {
                initializedResult = init();
            }
            catch
            {
                linkedInitialization435.Fail438();
                throw;
            }
            if (initializedResult != 0) return ErlDriverLoadErrors420.FailedInitialize;
            linkedInitialization435.Commit();
        }
        initialized = true;

        return ErlDriverLoadErrors420.NoError;
    }

    private async ValueTask<(ErlDriverImage420?, int)> Refuse(int error)
    {
        // Failed entry.init is not a successful driver, so finish is not called.
        // A genuine cleanup failure faults the host operation separately; it
        // must not masquerade as another driver's documented soft error code.
        await UnloadAsync().ConfigureAwait(false);

        return (null, error);
    }

    internal ErlDriverImageLease420 Acquire()
    {
        lock (gate)
        {
            if (retiring || !initialized) throw new ObjectDisposedException(ErlDriverLoadErrors420.ClosedImage);
            var lease = new ErlDriverImageLease420(this);
            active = checked(active + 1);

            return lease;
        }
    }

    internal ErlDriverImageLease420 AcquireWorker()
    {
        lock (gate)
        {
            if (workerAdmissionClosed) throw new ObjectDisposedException(ErlDriverLoadErrors420.ClosedImage);
            var lease = new ErlDriverImageLease420(this, worker: true);
            workers = checked(workers + 1);

            return lease;
        }
    }

    internal void Release(bool worker)
    {
        lock (gate)
        {
            if (worker)
            {
                if (workers <= 0) throw new InvalidOperationException(ErlDriverLoadErrors420.ActiveImage);
                workers--;
                if (workerAdmissionClosed && workers == 0) workersExited.TrySetResult();

                return;
            }
            if (active <= 0) throw new InvalidOperationException(ErlDriverLoadErrors420.ActiveImage);
            active--;
            if (retiring && active == 0) retire.TrySetResult(true);
        }
    }

    internal Task UnloadAsync()
    {
        lock (gate)
        {
            retiring = true;
            if (active == 0) retire.TrySetResult(true);

            return retirement;
        }
    }

    private async Task RetireCore()
    {
        _ = await retire.Task.ConfigureAwait(false);
        List<Exception>? failures = null;
        try
        {
            if (initialized)
            {
                using var invocation = enterOwner(this);
                using var role = new ErlDriverThreadRole420(false);
                Entry.Callback<ErlDriverEntryTypes420.Finish>(ErlDriverEntryNames420.Finish)?.Invoke();
            }
        }
        catch (Exception error)
        {
            (failures ??= []).Add(error);
        }
        finally
        {
            lock (gate)
            {
                workerAdmissionClosed = true;
                if (workers == 0) workersExited.TrySetResult();
            }
            // finish may be the callback which signals and joins real workers.
            // Release worker leases only after OS exit, including native TLS
            // destructors, never merely when the user thread function returns.
            await workersExited.Task.ConfigureAwait(false);
            try
            {
                int result = image == nint.Zero ? ErlDriverLoadErrors420.NoError : ErlDriverWindowsLoader420.Close(image);
                if (result != ErlDriverLoadErrors420.NoError)
                    (failures ??= []).Add(new System.ComponentModel.Win32Exception(ErlDriverLoadErrors420.DynamicOffset - result));
            }
            catch (Exception error)
            {
                (failures ??= []).Add(error);
            }
            finally
            {
                image = nint.Zero;
                if (tableRetained)
                {
                    tableRetained = false;
                    callbacks.DangerousRelease();
                }
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
