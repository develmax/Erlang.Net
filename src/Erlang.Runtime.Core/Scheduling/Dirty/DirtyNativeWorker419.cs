using System.Runtime.CompilerServices;
namespace Erlang;

internal sealed class DirtyNativeWorker419
{
    private readonly DirtyNativeOwner419 owner;
    private readonly SemaphoreSlim signal = new(0);
    private readonly Thread thread;
    private bool started;
    internal LinkedList<DirtyNativeInvocation419> Queue { get; } = new();
    internal TaskCompletionSource Quiesced { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal DirtyNativeKind419 Kind { get; }
    internal bool Alive => thread.IsAlive;
    internal int ThreadId => thread.ManagedThreadId;

    internal DirtyNativeWorker419(DirtyNativeOwner419 owner, DirtyNativeKind419 kind)
    {
        this.owner = owner;
        Kind = kind;
        thread = new(Run) { IsBackground = true, Name = kind == DirtyNativeKind419.Cpu ? DirtyNativeNames419.CpuThread : DirtyNativeNames419.IoThread };
    }

    internal void StartLocked()
    {
        DirtyNativeOwnerFlow419.StartThread(thread);
        started = true;
    }

    internal void SignalLocked() => signal.Release();

    internal void CloseLocked()
    {
        if (started) signal.Release(); else Quiesced.TrySetResult();
    }

    private void Run()
    {
        Exception? failure = null;
        try
        {
            while (true)
            {
                signal.Wait();
                if (!ExecuteOne()) break;
            }
        }
        catch (Exception error)
        {
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
            invocation.Execute(Kind, closing);
            owner.Completed(invocation);
        }

        return !owner.ClosedAndEmpty(this);
    }

    internal void Join()
    {
        if (started) thread.Join();
    }

    internal void DisposeSignal() => signal.Dispose();
}
