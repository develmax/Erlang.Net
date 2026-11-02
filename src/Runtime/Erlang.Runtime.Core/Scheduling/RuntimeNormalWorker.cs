namespace Erlang;

internal sealed class RuntimeNormalWorker
{
    private readonly Action pump;
    private readonly Action<Exception> reportFailure;
    private readonly RuntimeMicrostateAccounting? microstates;
    private readonly RuntimeUserCpuAccounting? userCpu;
    private readonly RuntimeWorkerCreationPolicy? creation;
    private readonly int identity;
    private readonly object lifetimeGate = new();
    private readonly SemaphoreSlim wake = new(0, 1);
    private readonly Thread thread;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource exited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int stopping;
    private bool started;
    private bool closing;
    private bool wakeDisposed;
    private int wakePending;
    private Task? join;

    internal Task Ready => ready.Task;

    internal RuntimeNormalWorker(
        int identity,
        RuntimeMicrostateAccounting? microstates,
        RuntimeUserCpuAccounting? userCpu,
        Action pump,
        Action<Exception> reportFailure
    )
        : this(
            identity,
            microstates,
            userCpu,
            pump,
            reportFailure,
            null
        )
    {
    }

    internal RuntimeNormalWorker(
        int identity,
        RuntimeMicrostateAccounting? microstates,
        RuntimeUserCpuAccounting? userCpu,
        Action pump,
        Action<Exception> reportFailure,
        RuntimeWorkerCreationPolicy? creation
    )
    {
        this.identity = identity;
        this.creation = creation;
        this.microstates = microstates;
        this.userCpu = userCpu;
        this.pump = pump;
        this.reportFailure = reportFailure;
        thread = new(Run) { IsBackground = true, Name = RuntimeNormalWorkerNames.Prefix + identity };
    }

    internal void Start()
    {
        lock (lifetimeGate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (started)
                return;
            creation?.BeforeStart(RuntimeWorkerCreationNames.Normal, identity);
            DirtyNativeOwnerFlow419.StartThread(thread);
            started = true;
        }
    }

    internal void Signal()
    {
        lock (lifetimeGate)
            if (!closing)
                SignalCore();
    }

    private void SignalCore()
    {
        if (!wakeDisposed && Interlocked.Exchange(ref wakePending, 1) == 0)
            wake.Release();
    }

    internal void Stop()
    {
        lock (lifetimeGate)
        {
            closing = true;
            Volatile.Write(ref stopping, 1);
            SignalCore();
        }
    }

    private void Run()
    {
        Exception? terminal = null;
        try
        {
            // Real physical thread lifetime includes startup/wake/loop work.
            // An OS sleep consumes no USER CPU; no synthetic time is added.
            using var cpu = userCpu?.Enter();
            creation?.BeforeAccounting(RuntimeWorkerCreationNames.Normal, identity);
            using var lifetime = microstates?.AttachConfiguredWorker(RuntimeMicrostateNames.Scheduler, identity, RuntimeMicrostate.Sleep);
            creation?.EnteredBeforeReady(RuntimeWorkerCreationNames.Normal, identity);
            ready.TrySetResult();
            while (true)
            {
                wake.Wait();
                Interlocked.Exchange(ref wakePending, 0);
                if (Volatile.Read(ref stopping) != 0)
                    break;
                try
                {
                    using var state = microstates is null ? null
                        : RuntimeMicrostateExecution.EnterState(microstates, RuntimeMicrostate.Other);
                    pump();
                }
                catch (Exception error)
                {
                    reportFailure(error);
                }
            }
        }
        catch (Exception error)
        {
            terminal = error;
            bool admitted = ready.Task.IsCompletedSuccessfully;
            ready.TrySetException(error);
            if (admitted)
                try
                {
                    reportFailure(error);
                }
                catch (Exception cleanup)
                {
                    terminal = new AggregateException(error, cleanup);
                }
        }
        finally
        {
            // Actual finally is not physical thread exit: Join remains mandatory.
            if (terminal is null)
                exited.TrySetResult();
            else
                exited.TrySetException(terminal);
        }
    }

    internal Task JoinAsync()
    {
        lock (lifetimeGate)
        {
            if (join is not null)
                return join;
            closing = true;
            Volatile.Write(ref stopping, 1);
            SignalCore();
            join = JoinCore(started);

            return join;
        }
    }

    private async Task JoinCore(bool actuallyStarted)
    {
        Exception? failure = null;
        if (actuallyStarted)
        {
            try
            {
                await exited.Task.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                failure = error;
            }
            await Task.Run(thread.Join).ConfigureAwait(false);
        }
        lock (lifetimeGate)
        {
            wakeDisposed = true;
            wake.Dispose();
        }
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
