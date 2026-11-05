namespace Erlang;

// A real dispatcher-owned reservation. It neither lends actor authority nor
// represents an ACK from an unmanaged native worker.
internal sealed class RuntimeMicrostateSourceTail1149
{
    internal enum Phase
    {
        Prepared,
        Withheld,
        FirstQueued,
        FirstRunning,
        FirstDone,
        SecondQueued,
        SecondRunning,
        Terminal
    }
    internal readonly RuntimeActorDispatcher Owner;
    internal readonly ProcessRuntime Runtime;
    internal readonly ProcessContext Requester;
    internal readonly RuntimeSchedulerAuxiliaryBatch420 Batch;
    internal readonly RuntimeMicrostateRequest Request;
    internal readonly Action Finish;
    internal readonly RuntimeSchedulerAuxiliary420 FirstWork, SecondWork;
    internal readonly InvalidOperationException Invalid = new(RuntimeNormalObservationNames.Requester);
    internal readonly ObjectDisposedException Closed = new(nameof(RuntimeMicrostateSourceTail1149));
    internal readonly OperationCanceledException RetirementCancellation;
    private readonly TaskCompletionSource first = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource settled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal Phase State;
    internal int Lane;
    internal Exception? Failure;
    internal Exception? CallbackFailure;
    private enum FailureSlot
    {
        Primary,
        Body,
        BodyReport,
        Abort,
        Release,
        Count
    }
    private readonly Exception?[] retirementFailures = new Exception?[(int)FailureSlot.Count];
    internal bool Registered;
    internal bool OwnsBatch;
    internal Task FirstCompletion => first.Task;
    internal Task Completion { get; }

    internal RuntimeMicrostateSourceTail1149(
        RuntimeActorDispatcher owner,
        ProcessContext requester,
        RuntimeSchedulerAuxiliaryBatch420 batch,
        RuntimeMicrostateRequest request,
        Action finish
    )
    {
        Owner = owner;
        Requester = requester;
        // Dispatcher retirement is committed before the runtime cancels actors.
        // Allocate the exact owner-token cause before registration or effects.
        RetirementCancellation = new(requester.Cancellation);
        Runtime = requester.Runtime;
        Batch = batch;
        Request = request;
        Finish = finish;
        FirstWork = new(lane => Execute(lane, true), Runtime);
        SecondWork = new(lane => Execute(lane, false), Runtime);
        // Register every observer before dispatcher admission/effect.
        Task lifetime = ObserveLifetime();
        Task firstObserver = ObserveWork(FirstWork.Completion);
        Task secondObserver = ObserveWork(SecondWork.Completion);
        if (lifetime.IsFaulted) lifetime.GetAwaiter().GetResult();
        if (firstObserver.IsFaulted) firstObserver.GetAwaiter().GetResult();
        if (secondObserver.IsFaulted) secondObserver.GetAwaiter().GetResult();
        Completion = Task.WhenAll(lifetime, firstObserver, secondObserver);
    }

    internal void QueueFirst() => Owner.QueueMicrostateSourceTail1149(this, true);

    internal void QueueSecond() => Owner.QueueMicrostateSourceTail1149(this, false);

    internal void Reject(Exception failure) => Owner.RejectMicrostateSourceTail1149(this, failure);

    private void Execute(int lane, bool isFirst)
    {
        if (!Owner.BeginMicrostateSourceTail1149(this, lane, isFirst)) return;
        Exception? failure = null;
        try
        {
            if (isFirst)
            {
                var original = Batch.Work[Lane - ProcessSchedulerNames.First];
                original.Execute(Lane);
                original.Completion.GetAwaiter().GetResult();
            }
            else Finish();
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            Owner.EndMicrostateSourceTail1149(this, isFirst, failure);
        }
    }

    private async Task ObserveWork(Task work)
    {
        try
        {
            await work.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            Owner.RejectMicrostateSourceTail1149(this, error);
        }
    }

    private async Task ObserveLifetime()
    {
        Exception? primary = null;
        try
        {
            await settled.Task.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            primary = error;
            retirementFailures[(int)FailureSlot.Primary] = error;
        }
        Exception? body = CallbackFailure;
        if (body is not null && !ReferenceEquals(body, primary))
        {
            retirementFailures[(int)FailureSlot.Body] = body;
            try
            {
                Request.Failure(body);
            }
            catch (Exception error)
            {
                retirementFailures[(int)FailureSlot.BodyReport] = error;
            }
        }
        try
        {
            if (primary is not null && OwnsBatch) Request.AbortStatisticsTrap(primary);
        }
        catch (Exception error)
        {
            retirementFailures[(int)FailureSlot.Abort] = error;
        }
        finally
        {
            try
            {
                Owner.ReleaseMicrostateSourceTail1149(this);
            }
            catch (Exception error)
            {
                retirementFailures[(int)FailureSlot.Release] = error;
            }
        }
        // Every ownership action was attempted before allocating an aggregate.
        Exception[] errors = retirementFailures.Where(error => error is not null).Cast<Exception>().ToArray();
        if (errors.Length == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Length != 0) throw new AggregateException(errors);
    }

    // The following methods are used ONLY while the actual dispatcher gate is held.
    internal void FirstSucceededUnderGate() => first.TrySetResult();

    internal void SettledUnderGate(Exception? error)
    {
        if (error is null)
        {
            first.TrySetResult();
            settled.TrySetResult();
        }
        else
        {
            first.TrySetException(error);
            settled.TrySetException(error);
        }
    }
}
