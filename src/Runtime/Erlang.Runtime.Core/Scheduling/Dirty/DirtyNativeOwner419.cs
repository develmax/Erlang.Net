namespace Erlang;

internal sealed partial class DirtyNativeOwner419
{
    private readonly object gate = new();
    private readonly DirtyNativeWorker419 cpu;
    private readonly DirtyNativeWorker419 io;
    private readonly DirtyNativeWorker419[] ioWorkers522;
    private readonly DirtyNativeWorker419[] workers522;
    private readonly Task quiescence522;
    private readonly List<Exception> retirementErrors522;
    internal int IoWorkerCount522 => ioWorkers522.Length;
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
            foreach (DirtyNativeWorker419 worker in workers522)
                worker.StartLocked();
            started = true;
        }
        catch
        {
            closed = true;
            foreach (DirtyNativeWorker419 worker in workers522)
                worker.CloseLocked();
            throw;
        }
    }

    internal DirtyNativeOwner419() : this(DirtyNativePoolParameters522.DefaultIoWorkers)
    {
    }

    internal DirtyNativeOwner419(int ioWorkerCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ioWorkerCount, DirtyNativePoolParameters522.MinimumIoWorkers);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(ioWorkerCount, DirtyNativePoolParameters522.MaximumIoWorkers);
        cpu = new(this, DirtyNativeKind419.Cpu);
        ioWorkers522 = new DirtyNativeWorker419[ioWorkerCount];
        workers522 = new DirtyNativeWorker419[checked(ioWorkerCount + 1)];
        workers522[0] = cpu;
        var sharedIo = new DirtyNativeQueue522();
        for (int index = 0; index < ioWorkers522.Length; index++)
        {
            ioWorkers522[index] = new(this, DirtyNativeKind419.Io, sharedIo);
            workers522[index + 1] = ioWorkers522[index];
        }
        io = ioWorkers522[0];
        // Allocate join bookkeeping before the owner can start/publish work.
        retirementErrors522 = new List<Exception>(checked(workers522.Length * 2 + 1));
        quiescence522 = Task.WhenAll(workers522.Select(worker => worker.Quiesced.Task).ToArray());
        Completion = DirtyNativeOwnerFlow419.Start(JoinAsync);
        if (Completion.IsFaulted)
            Completion.GetAwaiter().GetResult();
    }

    internal Task<Term> Enqueue(
        ProcessContext owner,
        DirtyNativeKind419 kind,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body,
        Action<Task>? physicalSettlement521 = null
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
                kind,
                module,
                function,
                arguments,
                body
            );
            LinkedListNode<DirtyNativeInvocation419>? node = null;
            try
            {
                // Record physical custody before any worker can see the call.
                physicalSettlement521?.Invoke(call.Settled.Task);
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
                foreach (DirtyNativeWorker419 worker in workers522)
                    worker.CloseLocked();
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
            foreach (DirtyNativeWorker419 worker in workers522)
                worker.CloseLocked();
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
            ioWorkers522.Any(worker => worker.Alive),
            cpu.ThreadId,
            io.ThreadId,
            Completion.IsCompletedSuccessfully
        );
    }

    private async Task JoinAsync()
    {
        List<Exception> failures = retirementErrors522;
        try
        {
            await quiescence522.ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        foreach (DirtyNativeWorker419 worker in workers522)
        {
            try
            {
                worker.Join();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        foreach (DirtyNativeWorker419 worker in workers522)
        {
            try
            {
                worker.DisposeSignal();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }
}
