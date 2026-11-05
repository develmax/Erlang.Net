namespace Erlang;

internal sealed partial class RuntimeActorDispatcher
{
    private readonly HashSet<RuntimeMicrostateSourceTail1149> sourceTails1149 = new();

    internal RuntimeMicrostateSourceTail1149 PrepareMicrostateSourceTail1149(
        ProcessContext context,
        RuntimeSchedulerAuxiliaryBatch420 batch,
        RuntimeMicrostateRequest request,
        Action finish
    )
    {
        ArgumentNullException.ThrowIfNull(finish);
        var tail = new RuntimeMicrostateSourceTail1149(
            this,
            context,
            batch,
            request,
            finish
        );
        lock (gate)
        {
            try
            {
                ObjectDisposedException.ThrowIf(retiring, this);
                if (!ReferenceEquals(batch.Owner, this) || batch.State != RuntimeSchedulerAuxiliaryState420.Prepared
                    || !context.Runtime.OwnsActorDispatcher428(this) || !RuntimeActorExecutionTurn.IsOwnedBy(context))
                    throw tail.Invalid;
                foreach (var existing in sourceTails1149)
                    if (ReferenceEquals(existing.Batch, batch)) throw tail.Invalid;
                sourceTails1149.Add(tail); // Allocation is before requested exchange.
                tail.OwnsBatch = true;
                tail.Registered = true;
            }
            catch (Exception failure)
            {
                RejectMicrostateSourceTailUnderGate1149(tail, failure);
                throw;
            }
        }

        return tail;
    }

    // Replace the old Publish call atomically, rather than binding a lane after
    // an await. The old implementation still owns normal/AUX publication.
    internal int? PublishMicrostatesWithSourceTail1149(ProcessContext context,
        RuntimeMicrostateSourceTail1149 tail)
    {
        lock (gate)
        {
            if (!ReferenceEquals(tail.Owner, this) || !ReferenceEquals(tail.Requester, context) || !tail.Registered
                || tail.State != RuntimeMicrostateSourceTail1149.Phase.Prepared) throw tail.Invalid;
            try
            {
                ObjectDisposedException.ThrowIf(retiring, this);
                int? lane = PublishMicrostateRequest(context, tail.Batch, tail.Request);
                if (lane is null)
                {
                    // Preserve the existing no-op trap disposal of its Prepared
                    // normal batch. No Ref/normal callback/source effect added.
                    tail.State = RuntimeMicrostateSourceTail1149.Phase.Terminal;
                    tail.FirstWork.Fail(tail.Closed);
                    tail.SecondWork.Fail(tail.Closed);
                    tail.SettledUnderGate(null);
                }
                else
                {
                    tail.Lane = lane.Value;
                    tail.State = RuntimeMicrostateSourceTail1149.Phase.Withheld;
                }

                return lane;
            }
            catch (Exception failure)
            {
                RejectMicrostateSourceTailUnderGate1149(tail, failure);
                throw;
            }
        }
    }

    internal void QueueMicrostateSourceTail1149(RuntimeMicrostateSourceTail1149 tail, bool first)
    {
        lock (gate)
        {
            if (!ReferenceEquals(tail.Owner, this)) throw tail.Invalid;
            // A genuine pending source RPC may resume after its own tail has
            // terminally retired and released registration. Preserve that actual
            // outcome rather than replacing it with an invalid-turn exception.
            if (tail.State == RuntimeMicrostateSourceTail1149.Phase.Terminal && tail.Failure is { } terminalFailure)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(terminalFailure);
            if (!tail.Registered) throw tail.Invalid;
            if (retiring)
            {
                RejectMicrostateSourceTailUnderGate1149(tail, tail.RetirementCancellation);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(tail.Failure ?? tail.RetirementCancellation);
            }
            var expected = first ? RuntimeMicrostateSourceTail1149.Phase.Withheld : RuntimeMicrostateSourceTail1149.Phase.FirstDone;
            if (tail.State != expected) throw tail.Invalid;
            var work = first ? tail.FirstWork : tail.SecondWork;
            tail.State = first ? RuntimeMicrostateSourceTail1149.Phase.FirstQueued : RuntimeMicrostateSourceTail1149.Phase.SecondQueued;
            try
            {
                lanes[tail.Lane - ProcessSchedulerNames.First].Auxiliary420.AddLast(work.Node);
                StartWorkers();
            }
            catch (Exception failure)
            {
                RejectMicrostateSourceTailUnderGate1149(tail, failure);
                throw;
            }
        }
    }

    internal bool BeginMicrostateSourceTail1149(RuntimeMicrostateSourceTail1149 tail, int lane, bool first)
    {
        lock (gate)
        {
            if (tail.State == RuntimeMicrostateSourceTail1149.Phase.Terminal) return false;
            var expected = first ? RuntimeMicrostateSourceTail1149.Phase.FirstQueued : RuntimeMicrostateSourceTail1149.Phase.SecondQueued;
            RuntimeMicrostateRecord? record = RuntimeMicrostateExecution.Current?.Record;
            if (!ReferenceEquals(tail.Owner, this) || !tail.Registered || tail.State != expected || lane != tail.Lane
                || !RuntimeNormalAuxiliaryTurn536.IsRuntime536(tail.Runtime) || RuntimeNormalAuxiliaryTurn536.Lane536 != lane
                || record is null || !record.Managed || record.Type != RuntimeMicrostateNames.Scheduler
                || record.Identity != lane || !ReferenceEquals(record.Owner, microstates)
                || !ReferenceEquals(record.PhysicalThread, RuntimeMicrostateThreadIdentity.Current))
                throw tail.Invalid;
            tail.State = first ? RuntimeMicrostateSourceTail1149.Phase.FirstRunning : RuntimeMicrostateSourceTail1149.Phase.SecondRunning;

            return true;
        }
    }

    internal void EndMicrostateSourceTail1149(RuntimeMicrostateSourceTail1149 tail, bool first, Exception? error)
    {
        lock (gate)
        {
            if (error is not null)
            {
                if (tail.Failure is not null && !ReferenceEquals(tail.Failure, error)) tail.CallbackFailure ??= error;
                tail.Failure ??= error;
            }
            if (tail.Failure is not null)
            {
                tail.State = RuntimeMicrostateSourceTail1149.Phase.Terminal;
                tail.FirstWork.Fail(tail.Failure);
                tail.SecondWork.Fail(tail.Failure);
                tail.Batch.Work[tail.Lane - ProcessSchedulerNames.First].Fail(tail.Failure);
                tail.SettledUnderGate(tail.Failure);
            }
            else if (first)
            {
                tail.State = RuntimeMicrostateSourceTail1149.Phase.FirstDone;
                tail.FirstSucceededUnderGate();
            }
            else
            {
                tail.State = RuntimeMicrostateSourceTail1149.Phase.Terminal;
                tail.SettledUnderGate(null);
            }
        }
    }

    internal void RejectMicrostateSourceTail1149(RuntimeMicrostateSourceTail1149 tail, Exception error)
    {
        lock (gate) RejectMicrostateSourceTailUnderGate1149(tail, error);
    }

    private void RejectMicrostateSourceTailUnderGate1149(RuntimeMicrostateSourceTail1149 tail, Exception error)
    {
        if (!ReferenceEquals(tail.Owner, this)) throw tail.Invalid;
        if (tail.State == RuntimeMicrostateSourceTail1149.Phase.Terminal) return;
        tail.Failure ??= error;
        tail.FirstWork.Node.List?.Remove(tail.FirstWork.Node);
        tail.SecondWork.Node.List?.Remove(tail.SecondWork.Node);
        if (tail.State is RuntimeMicrostateSourceTail1149.Phase.FirstRunning or RuntimeMicrostateSourceTail1149.Phase.SecondRunning)
            return; // A real running callback must return before physical completion.
        tail.State = RuntimeMicrostateSourceTail1149.Phase.Terminal;
        tail.FirstWork.Fail(tail.Failure);
        tail.SecondWork.Fail(tail.Failure);
        if (tail.OwnsBatch && tail.Batch.State == RuntimeSchedulerAuxiliaryState420.Prepared)
        {
            tail.Batch.State = RuntimeSchedulerAuxiliaryState420.Aborted;
            foreach (var work in tail.Batch.Work) work.Fail(tail.Failure);
        }
        else if (tail.OwnsBatch && tail.Lane != 0) tail.Batch.Work[tail.Lane - ProcessSchedulerNames.First].Fail(tail.Failure);
        tail.SettledUnderGate(tail.Failure);
    }

    private void RetireMicrostateSourceTailsUnderGate1149()
    {
        foreach (var tail in sourceTails1149)
            RejectMicrostateSourceTailUnderGate1149(tail, tail.RetirementCancellation);
    }

    private void RejectAllMicrostateSourceTailsUnderGate1149(Exception error)
    {
        foreach (var tail in sourceTails1149) RejectMicrostateSourceTailUnderGate1149(tail, error);
    }

    internal void ReleaseMicrostateSourceTail1149(RuntimeMicrostateSourceTail1149 tail)
    {
        lock (gate)
        {
            if (!tail.Registered) return;
            if (tail.State != RuntimeMicrostateSourceTail1149.Phase.Terminal) throw tail.Invalid;
            sourceTails1149.Remove(tail);
            tail.Registered = false;
            CompleteIfIdle();
        }
    }
}
