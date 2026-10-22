namespace Erlang;

internal sealed class DirtyNativeOwner419
{
    private readonly object gate = new();
    private readonly DirtyNativeWorker419 cpu, io;
    private readonly HashSet<DirtyNativeInvocation419> invocations = new();
    private bool started, closed;
    internal Task Completion { get; }

    internal void EnsureStarted()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, DirtyNativeNames419.Closed);
            StartLocked();
        }
    }

    private void StartLocked()
    {
        if (started) return;
        try
        {
            cpu.StartLocked();
            io.StartLocked();
            started = true;
        }
        catch
        {
            closed = true;
            cpu.CloseLocked();
            io.CloseLocked();
            throw;
        }
    }

    internal DirtyNativeOwner419()
    {
        cpu = new(this, DirtyNativeKind419.Cpu);
        io = new(this, DirtyNativeKind419.Io);
        Completion = DirtyNativeOwnerFlow419.Start(JoinAsync);
    }

    internal Task<Term> Enqueue(
        ProcessContext owner,
        DirtyNativeKind419 kind,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body
    )
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closed, DirtyNativeNames419.Closed);
            owner.Cancellation.ThrowIfCancellationRequested();
            DirtyNativeWorker419 worker = kind switch
            {
                DirtyNativeKind419.Cpu => cpu,
                DirtyNativeKind419.Io => io,
                _ => throw new ArgumentException(DirtyNativeNames419.BadKind)
            };
            StartLocked();
            invocations.EnsureCapacity(checked(invocations.Count + 1));
            var call = new DirtyNativeInvocation419(
                owner,
                module,
                function,
                arguments,
                body
            );
            LinkedListNode<DirtyNativeInvocation419>? node = null;
            try
            {
                node = new(call);
                invocations.Add(call);
                worker.Queue.AddLast(node);
                worker.SignalLocked();
            }
            catch (Exception error)
            {
                if (node?.List is not null) worker.Queue.Remove(node);
                invocations.Remove(call);
                call.FailBeforeExecution(error);
                call.MarkSettled();
                throw;
            }

            return call.Outcome.Task;
        }
    }

    internal Task BeginClose()
    {
        lock (gate)
        {
            if (!closed)
            {
                closed = true;
                cpu.CloseLocked();
                io.CloseLocked();
            }

            return Completion;
        }
    }

    internal Task DrainProcess(ProcessContext context)
    {
        lock (gate) return Task.WhenAll(invocations.Where(call => ReferenceEquals(call.Owner, context)).Select(call => call.Settled.Task));
    }

    internal bool TryTake(DirtyNativeWorker419 worker, out DirtyNativeInvocation419? call, out bool closing)
    {
        lock (gate)
        {
            closing = closed;
            call = worker.Queue.First?.Value;
            if (call is not null) worker.Queue.RemoveFirst();
            if (call is not null) call.Running = true;

            return call is not null || !closed;
        }
    }

    internal bool ClosedAndEmpty(DirtyNativeWorker419 worker)
    {
        lock (gate) return closed && worker.Queue.Count == 0;
    }

    internal void Completed(DirtyNativeInvocation419 call)
    {
        lock (gate) invocations.Remove(call);
        call.MarkSettled();
    }

    internal void FailWorker(Exception failure)
    {
        List<DirtyNativeInvocation419> abandoned = new();
        lock (gate)
        {
            closed = true;
            while (cpu.Queue.First is { } c)
            {
                abandoned.Add(c.Value);
                cpu.Queue.RemoveFirst();
            }
            while (io.Queue.First is { } i)
            {
                abandoned.Add(i.Value);
                io.Queue.RemoveFirst();
            }
            cpu.CloseLocked();
            io.CloseLocked();
        }
        foreach (var call in abandoned)
        {
            call.FailBeforeExecution(failure);
            Completed(call);
        }
    }

    internal bool HasDirtyWork428(ProcessContext context)
    {
        lock (gate) return invocations.Any(call => ReferenceEquals(call.Owner, context));
    }

    internal bool IsExecuting423(ProcessContext context)
    {
        lock (gate) return invocations.Any(call => ReferenceEquals(call.Owner, context) && call.PhysicallyExecuting423);
    }

    internal DirtyNativeSnapshot419 Snapshot()
    {
        lock (gate) return new(
            closed,
            invocations.Count(call => !call.Running),
            invocations.Count(call => call.Running),
            cpu.Alive,
            io.Alive,
            cpu.ThreadId,
            io.ThreadId,
            Completion.IsCompletedSuccessfully
        );
    }

    private async Task JoinAsync()
    {
        var failures = new List<Exception>();
        try
        {
            await Task.WhenAll(cpu.Quiesced.Task, io.Quiesced.Task).ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            cpu.Join();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            io.Join();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            cpu.DisposeSignal();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            io.DisposeSignal();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
