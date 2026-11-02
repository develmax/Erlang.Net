using System.Runtime.CompilerServices;
namespace Erlang;

internal sealed class DirtyNativeWorker419
{
    private readonly DirtyNativeOwner419 owner;
    private readonly DirtyNativeQueue522 queue522;
    private int releasedQueue522;
    private bool joined522;
    private readonly Thread thread;
    private readonly RuntimeWorkerCreationPolicy? creation;
    private readonly int identity;
    private bool started;
    private readonly RuntimeDirtyWallTime wallTime = new();
    internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal RuntimeDirtyWallTimeSample SampleWallTime() => wallTime.Sample();

    internal LinkedList<DirtyNativeInvocation419> Queue => queue522.Items;
    internal bool Joined522 => Volatile.Read(ref joined522);
    internal TaskCompletionSource Quiesced { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal DirtyNativeKind419 Kind { get; }
    internal bool Alive => thread.IsAlive;
    internal int ThreadId => thread.ManagedThreadId;

    internal DirtyNativeWorker419(DirtyNativeOwner419 owner, DirtyNativeKind419 kind, DirtyNativeQueue522? sharedQueue = null)
        : this(
            owner,
            kind,
            sharedQueue,
            null,
            1
        )
    {
    }

    internal DirtyNativeWorker419(
        DirtyNativeOwner419 owner,
        DirtyNativeKind419 kind,
        DirtyNativeQueue522? sharedQueue,
        RuntimeWorkerCreationPolicy? creation,
        int identity
    )
    {
        this.creation = creation;
        this.identity = identity;
        queue522 = sharedQueue ?? new DirtyNativeQueue522();
        queue522.AddMember();
        this.owner = owner;
        Kind = kind;
        thread = new(Run) { IsBackground = true, Name = kind == DirtyNativeKind419.Cpu ? DirtyNativeNames419.CpuThread : DirtyNativeNames419.IoThread };
    }

    internal void StartLocked()
    {
        creation?.BeforeStart(Kind == DirtyNativeKind419.Cpu ? RuntimeWorkerCreationNames.DirtyCpu : RuntimeWorkerCreationNames.DirtyIo, identity);
        DirtyNativeOwnerFlow419.StartThread(thread);
        started = true;
    }

    internal void SignalLocked() => queue522.Signal.Release();

    internal void CloseLocked()
    {
        if (started) queue522.Signal.Release(); else Quiesced.TrySetResult();
    }

    private void Run()
    {
        Exception? failure = null;
        try
        {
            wallTime.InitializeOnWorker();
            creation?.BeforeAccounting(Kind == DirtyNativeKind419.Cpu ? RuntimeWorkerCreationNames.DirtyCpu : RuntimeWorkerCreationNames.DirtyIo, identity);
            using var microstate = owner.Microstates?.AttachConfiguredWorker(
                Kind == DirtyNativeKind419.Cpu
                ? RuntimeMicrostateNames.DirtyCpu : RuntimeMicrostateNames.DirtyIo,
                identity,
                RuntimeMicrostate.Sleep
            );
            creation?.EnteredBeforeReady(Kind == DirtyNativeKind419.Cpu ? RuntimeWorkerCreationNames.DirtyCpu : RuntimeWorkerCreationNames.DirtyIo, identity);
            Started.TrySetResult();
            while (true)
            {
                queue522.Signal.Wait();
                if (!ExecuteOne()) break;
            }
        }
        catch (Exception error)
        {
            Started.TrySetException(error);
            failure = error;
            try
            {
                owner.FailWorker(error);
            }
            catch (Exception cleanup)
            {
                failure = new AggregateException(error, cleanup);
            }
        }
        finally
        {
            if (failure is null) Quiesced.TrySetResult(); else Quiesced.TrySetException(failure);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ExecuteOne()
    {
        // Deliberately no last-invocation local in the idle blocking Run frame.
        if (!owner.TryTake(this, out var invocation, out bool closing)) return false;
        if (invocation is not null)
        {
            wallTime.BeginWork();
            try
            {
                using var state = owner.Microstates is null ? null
                    : RuntimeMicrostateExecution.EnterState(owner.Microstates, RuntimeMicrostate.Emulator);
                invocation.Execute(Kind, closing);
                owner.Completed(invocation);
            }
            finally
            {
                wallTime.EndWork();
            }
        }

        return !owner.ClosedAndEmpty(this);
    }

    internal void Join()
    {
        if (started) thread.Join();
        Volatile.Write(ref joined522, true);
    }

    internal void DisposeSignal()
    {
        if (Interlocked.Exchange(ref releasedQueue522, 1) == 0)
            queue522.RemoveMember();
    }
}
