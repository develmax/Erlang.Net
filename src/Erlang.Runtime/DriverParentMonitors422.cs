namespace Erlang;

// These are driver origins, not Erlang process-monitor mailbox DOWNs. A target
// death queues the actual child process_exit callback; it does not prematurely
// erase the reference the callback may query/demonitor. The callback ACK or
// explicit demonitor removes it. The worker has no guest ExecutionContext.
internal sealed class DriverParentMonitors422 : IErlDriverMonitorHost420
{
    private readonly ProcessRuntime runtime;
    private readonly object gate;
    private readonly DriverParentActorHandles422 actors;
    private readonly ErlDriverHostPeer420 peer;
    private readonly NativeDriverPortRegistry422 ports;
    private readonly Dictionary<(ErlDriverPortIdentity420 Port, ulong Attempt), DriverMonitorOrigin422> origins = new();
    private readonly Dictionary<Pid, HashSet<DriverMonitorOrigin422>> targets = new();
    private readonly Queue<DriverMonitorOrigin422> deaths = new();
    private readonly SemaphoreSlim wake = new(0);
    private Task worker = Task.CompletedTask;
    private bool workerStarted;
    private bool closing;

    internal Task Completion => worker;

    internal DriverParentMonitors422(
        ProcessRuntime runtime,
        object gate,
        DriverParentActorHandles422 actors,
        ErlDriverHostPeer420 peer,
        NativeDriverPortRegistry422 ports
    )
    {
        this.runtime = runtime;
        this.gate = gate;
        this.actors = actors;
        this.peer = peer;
        this.ports = ports;
    }

    internal void StartWorker()
    {
        if (workerStarted) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            worker = Run();
            workerStarted = true;
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    public ErlDriverMonitorRegistration420 Register(
        ErlDriverPortIdentity420 port,
        ulong attempt,
        ulong targetActor,
        Pid target
    )
    {
        if (attempt == 0 || targetActor == 0) throw new InvalidDataException(DriverParentWireNames422.Invalid);
        // ETF carries the current diagnostic representation. The authenticated
        // token is authoritative across a real local-node rename.
        Pid actual = actors.Resolve(targetActor);
        if (actual.Id != target.Id)
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            ports.RequireNativeOrigin(port);
            if (!runtime.DriverTargetInternal422(actual)) return new(-1, null);
            if (!runtime.DriverTargetAlive422(actual)) return new(1, null);
            ReferenceTerm reference = runtime.MakeReference();
            byte[] prefix = ErlDriverMonitorLayout420.Encode(reference);
            var origin = new DriverMonitorOrigin422(
                port,
                attempt,
                actual,
                reference
            );
            origins.Add((port, attempt), origin);
            try
            {
                if (!targets.TryGetValue(actual, out var rows)) targets.Add(actual, rows = new());
                try
                {
                    rows.Add(origin);
                }
                catch
                {
                    if (rows.Count == 0) targets.Remove(actual);
                    throw;
                }
            }
            catch
            {
                origins.Remove((port, attempt));
                throw;
            }

            return new(0, prefix);
        }
    }

    public int Unregister(ErlDriverPortIdentity420 port, ulong attempt)
    {
        lock (gate)
        {
            if (!origins.TryGetValue((port, attempt), out var origin)) return 1;
            Remove(origin);

            return 0;
        }
    }

    private void Remove(DriverMonitorOrigin422 origin)
    {
        origin.Removed = true;
        origins.Remove((origin.Port, origin.Attempt));
        if (targets.TryGetValue(origin.Target, out var rows))
        {
            rows.Remove(origin);
            if (rows.Count == 0) targets.Remove(origin.Target);
        }
    }

    // Actual Terminate hook under the shared runtime gate, after the process is
    // marked dead. Never awaits IPC/ACK while that gate or Register is active.
    internal void ProcessTerminated(Pid target)
    {
        lock (gate)
        {
            if (!targets.TryGetValue(target, out var rows)) return;
            foreach (var origin in rows)
            {
                if (origin.Removed || origin.DeathQueued) continue;
                deaths.Enqueue(origin);
                origin.DeathQueued = true;
                wake.Release();
            }
        }
    }

    internal void PortStopped(ErlDriverPortIdentity420 port)
    {
        lock (gate)
            foreach (var origin in origins.Values.Where(origin => origin.Port == port).ToArray()) Remove(origin);
    }

    internal Task BeginClose()
    {
        lock (gate)
        {
            // Called only after actual port cleanup ACKs, while peer/dispatcher
            // remain alive. Any in-flight callback response is joined by Run.
            closing = true;
            foreach (var origin in origins.Values) origin.Removed = true;
            origins.Clear();
            targets.Clear();
            wake.Release();

            return worker;
        }
    }

    private async Task Run()
    {
        while (true)
        {
            await wake.WaitAsync().ConfigureAwait(false);
            DriverMonitorOrigin422? origin;
            lock (gate)
            {
                if (!deaths.TryDequeue(out origin))
                {
                    if (closing) return;
                    continue;
                }
                if (origin.Removed) continue;
            }
            try
            {
                // Deliberately outside a retained normal permit. Child posts
                // the actual callback back through SchedulePortTask; retaining
                // the only lane here would deadlock its own ACK.
                var response = await peer.RequestAsync(
                    ErlDriverHostOperation420.ProcessExited,
                    origin.Port.Driver,
                    origin.Port.Port,
                    0,
                    0,
                    null,
                    ErlDriverPrimitiveWire420.Encode(origin.Attempt),
                    CancellationToken.None
                ).ConfigureAwait(false);
                if (response.NativeResult != 0 || response.Data is not null)
                    throw new InvalidDataException(DriverParentWireNames422.Invalid);
            }
            finally
            {
                lock (gate) Remove(origin);
            }
            origin = null;
        }
    }
}
