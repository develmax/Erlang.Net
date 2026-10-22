namespace Erlang;

// Native readiness producers ask the actual parent VM dispatcher for a turn.
// Only its authenticated InvokePortTask permit invokes a native callback. This
// preserves the normal scheduler role without inventing one on a timer thread.
internal sealed class ErlDriverPortJobs420
{
    private readonly object gate = new();
    private readonly Dictionary<ulong, ErlDriverPortJob420> jobs = new();
    private readonly ErlDriverHostPeer420 peer;
    private readonly ErlDriverServiceFault420 fault;
    private ulong next;
    private bool closing;

    internal ErlDriverPortJobs420(ErlDriverHostPeer420 peer, ErlDriverServiceFault420 fault)
    {
        this.peer = peer;
        this.fault = fault;
    }

    internal Task Post(
        ErlDriverPortIdentity420 port,
        ErlDriverPortTaskKind420 kind,
        Func<bool, bool> invoke,
        Action? drop = null
    )
    {
        ErlDriverPortJob420 job;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            ulong identity = checked(next + 1);
            job = new(
                identity,
                port,
                kind,
                invoke,
                drop
            );
            jobs.Add(identity, job);
            next = identity;
            try
            {
                bool suppress = !ExecutionContext.IsFlowSuppressed();
                AsyncFlowControl flow = default;
                try
                {
                    if (suppress) flow = ExecutionContext.SuppressFlow();
                    _ = Drive(job);
                }
                finally
                {
                    if (suppress) flow.Undo();
                }
            }
            catch
            {
                jobs.Remove(identity);
                throw;
            }
        }
        job.Start.TrySetResult();

        return job.Done.Task;
    }

    private async Task Drive(ErlDriverPortJob420 job)
    {
        await job.Start.Task.ConfigureAwait(false);
        Exception? failure = null;
        try
        {
            var reply = await peer.RequestAsync(
                ErlDriverHostOperation420.SchedulePortTask,
                job.Port.Driver,
                job.Port.Port,
                0,
                (uint)job.Kind,
                null,
                ErlDriverPrimitiveWire420.Encode(job.Identity),
                CancellationToken.None
            ).ConfigureAwait(false);
            if (reply.NativeResult != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            lock (gate)
                if (!job.Invoked && !job.Canceled) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        }
        catch (Exception error)
        {
            failure = error;
            fault.Record(error);
        }
        finally
        {
            bool invoked;
            lock (gate)
            {
                invoked = job.Invoked;
                if (!invoked) job.Canceled = true;
            }
            // A transport failure can settle SchedulePortTask while an already
            // admitted callback still owns its native data. Never free underneath it.
            if (invoked) await job.InvocationDone.Task.ConfigureAwait(false);
            try
            {
                Drop(job);
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
                fault.Record(error);
            }
            lock (gate)
            {
                if (failure is null) job.Done.TrySetResult();
                else job.Done.TrySetException(failure);
                jobs.Remove(job.Identity);
            }
        }
    }

    private void Drop(ErlDriverPortJob420 job)
    {
        Action? cleanup;
        lock (gate)
        {
            if (job.Dropped) return;
            job.Dropped = true;
            cleanup = job.Drop;
        }
        cleanup?.Invoke();
    }

    internal ErlDriverHostFrame420 Invoke(ErlDriverHostFrame420 request)
    {
        if (request.Operation != ErlDriverHostOperation420.InvokePortTask || request.Actor != 0
            || request.Name is not null || !request.SchedulerThread)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        ulong identity = ErlDriverPrimitiveWire420.Decode<ulong>(request.Data);
        ErlDriverPortJob420 job;
        bool canceled;
        lock (gate)
        {
            if (!jobs.TryGetValue(identity, out job!) || job.Invoked || request.Driver != job.Port.Driver
                || request.Port != job.Port.Port || request.Command != (uint)job.Kind)
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            job.Invoked = true;
            canceled = job.Canceled;
        }
        try
        {
            using var nativeRole = new ErlDriverThreadRole420(request.SchedulerThread);
            bool delivered = false;
            if (!canceled)
            {
                try
                {
                    delivered = job.Invoke(request.SchedulerThread);
                }
                catch (ErlDriverSourceBadArgument420) { }
            }
            if (delivered)
            {
                // Invoke owns data after successful delivery (ready_async callback
                // gets its real ownership); a skipped callback uses Drop instead.
                lock (gate) job.Dropped = true;
            }
            else Drop(job);

            return request with { Kind = ErlDriverHostKind420.Response, NativeResult = 0, Name = null, Data = null };
        }
        finally
        {
            job.InvocationDone.TrySetResult();
        }
    }

    internal Task CloseAsync()
    {
        lock (gate)
        {
            closing = true;
            foreach (var job in jobs.Values)
                if (!job.Invoked) job.Canceled = true;

            return Task.WhenAll(jobs.Values.Select(job => job.Done.Task));
        }
    }
}
