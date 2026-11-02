using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Erlang;

// An acknowledged NIF-only actor continuation. Queue failure releases only
// unclaimed admission; fallback resumes solely to throw from GetResult.
internal sealed class NifActorAdmission616<T> : ICriticalNotifyCompletion
{
    private readonly Task ready;
    private readonly ProcessContext? actor;
    private readonly Func<T> claim;
    private readonly Action<Exception> reject;
    private readonly ExecutionContext? execution;
    private readonly RuntimeActorContinuation request;
    private readonly Action resume;
    private readonly Action<Task> observe;
    private Action? continuation;
    private Exception? failure;
    private int started;
    private int delivered;
    private readonly ContextCallback runInContext;
    private readonly WaitCallback fallback;

    internal NifActorAdmission616(
        Task ready,
        ProcessContext? actor,
        Func<T> claim,
        Action<Exception> reject
    )
    {
        this.ready = ready;
        this.actor = actor;
        this.claim = claim;
        this.reject = reject;
        execution = ExecutionContext.Capture();
        request = new(Run616, ProcessExecutionAdmission.Reentry);
        resume = Resume616;
        observe = Observe616;
        runInContext = _ => InvokeOnce616();
        fallback = _ => Run616();
    }

    public NifActorAdmission616<T> GetAwaiter() => this;

    public bool IsCompleted => ready.IsCompleted
        && (actor is null || RuntimeActorExecutionTurn.IsOwnedBy(actor));

    public T GetResult()
    {
        try
        {
            if (failure is { } error) ExceptionDispatchInfo.Capture(error).Throw();
            ready.GetAwaiter().GetResult();
            if (actor is not null && !RuntimeActorExecutionTurn.IsOwnedBy(actor))
                throw new InvalidOperationException(RuntimeNormalBarrierNames696.Requester);

            return claim();
        }
        catch (Exception error)
        {
            reject(error);
            throw;
        }
    }

    public void OnCompleted(Action body) => UnsafeOnCompleted(body);

    public void UnsafeOnCompleted(Action body)
    {
        continuation = body;
        if (Volatile.Read(ref failure) is not null)
        {
            ThreadPool.UnsafeQueueUserWorkItem(fallback, null);

            return;
        }
        try
        {
            ready.GetAwaiter().UnsafeOnCompleted(resume);
        }
        catch (Exception error)
        {
            Reject616(error);
        }
    }

    internal void RejectExternal616(Exception error)
    {
        Interlocked.CompareExchange(ref failure, error, null);
        try
        {
            reject(error);
        }
        finally
        {
            // Actor exit/shutdown may run under runtime locks. Never execute
            // the failed caller's cleanup inline on that cancellation thread.
            if (Volatile.Read(ref continuation) is not null)
                ThreadPool.UnsafeQueueUserWorkItem(fallback, null);
        }
    }

    private void Resume616()
    {
        if (actor is null)
        {
            Run616();

            return;
        }
        try
        {
            actor.Cancellation.ThrowIfCancellationRequested();
            Task queued = actor.ActorTaskScheduler!.ContinueNifAdmission616(request);
            _ = queued.ContinueWith(
                observe,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }
        catch (Exception error)
        {
            Reject616(error);
        }
    }

    private void Observe616(Task queued)
    {
        if (queued.IsFaulted)
            Reject616(queued.Exception!.InnerException ?? queued.Exception);
        else if (queued.IsCanceled)
            Reject616(new TaskCanceledException(queued));
    }

    private void Reject616(Exception error)
    {
        Interlocked.CompareExchange(ref failure, error, null);
        try
        {
            // The resource ticket itself decides unclaimed versus claimed.
            // A started queue action is not proof that GetResult claimed it.
            reject(error);
        }
        finally
        {
            if (Volatile.Read(ref started) == 0)
                Run616();
            else
                InvokeOnce616();
        }
    }

    private void InvokeOnce616()
    {
        if (Interlocked.CompareExchange(ref delivered, 1, 0) == 0)
            continuation!();
    }

    private void Run616()
    {
        if (Interlocked.CompareExchange(ref started, 1, 0) != 0) return;
        try
        {
            if (execution is null)
                InvokeOnce616();
            else
                ExecutionContext.Run(execution, runInContext, null);
        }
        catch (Exception error)
        {
            // If context entry failed before MoveNext, fallback delivers only
            // its failed GetResult; an already delivered action is never rerun.
            Reject616(error);
        }
    }
}
