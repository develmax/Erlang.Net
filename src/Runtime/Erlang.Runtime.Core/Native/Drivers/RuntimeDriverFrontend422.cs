namespace Erlang;

// One actual runtime owns one authenticated native child. No default public
// registration occurs here. The explicit bootstrap publisher admits bodies
// only after Initialize has completed against the configured, verified package.
internal sealed partial class RuntimeDriverFrontend422
{
    private readonly ProcessRuntime runtime;
    private readonly object gate;
    private readonly DriverFrontendConfiguration422 configuration;
    private readonly ErlDriverHostChild420 child;
    private readonly DriverDispatcherLease422 dispatcher;
    private readonly RuntimeOsEnvironmentDriverLease422 environment;
    private readonly DriverParentActorHandles422 actors;
    private readonly DriverParentPeerTransport422 transport;
    private readonly RuntimeDdllParent422 drivers;
    private readonly NativeDriverPortRegistry422 ports;
    private readonly DriverParentMonitors422 monitors;
    private readonly ErlDriverPrimitiveParent420 primitives;
    private readonly ErlDriverPortTaskParent420 tasks;
    private readonly TaskCompletionSource retirementRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task retirement;
    private bool ready;
    private bool closing;
    private Task supervision = Task.CompletedTask;
    private Exception? fatal;

    internal bool RetirementCompletedSuccessfully => retirement.IsCompletedSuccessfully;

    internal void RequireReady()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (!ready) throw new InvalidOperationException(DriverParentWireNames422.Invalid);
        }
    }

    internal Task ProcessTerminated(
        ProcessContext context,
        Term reason,
        RuntimeProcessPayload641? reason641 = null
    )
    {
        var work = new List<Task>();
        try
        {
            monitors.ProcessTerminated(context.Self);
        }
        catch (Exception failure)
        {
            work.Add(Task.FromException(failure));
        }
        try
        {
            work.Add(drivers.ProcessTerminated(context.Self));
        }
        catch (Exception failure)
        {
            work.Add(Task.FromException(failure));
        }
        try
        {
            work.Add(ports.OwnerTerminated(
                context.Self,
                reason,
                transport,
                reason641
            ));
        }
        catch (Exception failure)
        {
            work.Add(Task.FromException(failure));
        }

        return Task.WhenAll(work);
    }

    private RuntimeDriverFrontend422(
        ProcessRuntime runtime,
        object gate,
        DriverFrontendConfiguration422 configuration,
        ErlDriverHostChild420 child,
        DriverDispatcherLease422 dispatcher,
        RuntimeOsEnvironmentDriverLease422 environment
    )
    {
        this.runtime = runtime;
        this.gate = gate;
        this.configuration = configuration;
        this.child = child;
        this.dispatcher = dispatcher;
        this.environment = environment;
        actors = new();
        transport = new(
            runtime,
            child.Peer,
            actors,
            dispatcher
        );
        NativeDriverPortRegistry422? actualPorts = null;
        drivers = new(
            runtime,
            gate,
            transport,
            row => actualPorts!.KillDriver(row, transport)
        );
        ports = actualPorts = new(
            runtime,
            gate,
            drivers,
            dispatcher
        );
        ports.BindFailure(RecordFailure);
        transport.BindPortDispatcher(ports.Dispatch);
        monitors = new(
            runtime,
            gate,
            actors,
            child.Peer,
            ports
        );
        primitives = new(new ErlDriverRuntimePrimitiveHost422(runtime, configuration.SystemInfo, environment));
        tasks = new(child.Peer, ports);
        bool suppress = !ExecutionContext.IsFlowSuppressed();
        AsyncFlowControl flow = default;
        try
        {
            if (suppress) flow = ExecutionContext.SuppressFlow();
            retirement = RetireOnRequest();
        }
        finally
        {
            if (suppress) flow.Undo();
        }
    }

    internal static async ValueTask<RuntimeDriverFrontend422> Create(
        ProcessRuntime runtime,
        object gate,
        DriverFrontendConfiguration422 configuration,
        CancellationToken cancellation
    )
    {
        cancellation.ThrowIfCancellationRequested();
        var dispatcher = runtime.AcquireDriverDispatcher422();
        RuntimeOsEnvironmentDriverLease422? environment = null;
        ErlDriverHostChild420? child = null;
        RuntimeDriverFrontend422? owner = null;
        var prepared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            environment = runtime.RequireOsEnvironment().AcquireDriverLease422();
            child = await ErlDriverHostChild420.StartAsync(
                configuration.Package,
                configuration.Launch,
                async (request, token) =>
                {
                    await prepared.Task.ConfigureAwait(false);

                    return await owner!.Handle(request, token).ConfigureAwait(false);
                },
                async (message, token) =>
                {
                    await prepared.Task.ConfigureAwait(false);
                    await owner!.Event(message, token).ConfigureAwait(false);
                },
                cancellation
            ).ConfigureAwait(false);
            owner = new(
                runtime,
                gate,
                configuration,
                child,
                dispatcher,
                environment
            );
            owner.drivers.StartWorker();
            owner.monitors.StartWorker();
            owner.StartSupervision();
            prepared.TrySetResult();
            var reply = await child.Peer.RequestAsync(
                ErlDriverHostOperation420.Initialize,
                0,
                0,
                0,
                0,
                null,
                ErlDriverPrimitiveWire420.Encode(
                    new ErlDriverInitializeRequest420(configuration.SystemInfo, configuration.StartupDirectory)),
                cancellation
            ).ConfigureAwait(false);
            if (reply.NativeResult != 0 || reply.Data is not null)
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            lock (gate)
            {
                cancellation.ThrowIfCancellationRequested();
                if (owner.closing) throw new IOException(DriverParentWireNames422.Closed);
                owner.ready = true;
            }

            return owner;
        }
        catch (Exception initial)
        {
            if (initial is ErlDriverHostStartupFailure423 ownership) child = ownership.Child;
            prepared.TrySetException(initial);
            _ = prepared.Task.Exception;
            var failures = new List<Exception> { initial };
            if (owner is not null)
            {
                try
                {
                    await owner.BeginClose().ConfigureAwait(false);
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            }
            else
            {
                if (child is not null)
                {
                    try
                    {
                        await child.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception failure)
                    {
                        failures.Add(failure);
                    }
                    // A typed failed Start transfers the actual still-owned
                    // child. A faulted disposal is never a physical fence.
                    await child.PhysicalExit.ConfigureAwait(false);
                    try
                    {
                        await child.PhysicalCleanup.ConfigureAwait(false);
                    }
                    catch (Exception failure)
                    {
                        failures.Add(failure);
                    }
                }
                try
                {
                    environment?.Dispose();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
                try
                {
                    dispatcher.Dispose();
                }
                catch (Exception failure)
                {
                    failures.Add(failure);
                }
            }
            if (failures.Count != 1) throw new AggregateException(failures);
            throw;
        }
    }

    internal IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> CreateDdllBodies()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(closing, this);
            if (!ready) throw new InvalidOperationException(DriverParentWireNames422.Invalid);

            return DdllNativeInvocation422.Create(runtime, drivers);
        }
    }

    internal Task BeginClose()
    {
        lock (gate)
        {
            closing = true;
            ready = false;
            retirementRequested.TrySetResult();

            return retirement;
        }
    }
}
