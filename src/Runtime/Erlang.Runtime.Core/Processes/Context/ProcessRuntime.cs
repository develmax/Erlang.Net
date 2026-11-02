namespace Erlang;

public sealed partial class ProcessRuntime : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<Pid, ProcessContext> processes = new();
    private readonly HashSet<ProcessContext> executingProcesses = new();
    private readonly SemaphoreSlim codePurger = new(1, 1);
    private readonly ProcessSlotTable processSlots;
    public int ProcessLimit => processSlots.Capacity;
    public int ReferenceDomainCapacity => Math.Min(ActorDispatchCapacity, (int)ReferenceNumberLayout.DomainMask);
    private readonly Dictionary<string, Term> names = new(StringComparer.Ordinal);
    internal RuntimePorts Ports
    {
        get;
    }
    internal CpuTopologyState CpuTopology { get; } = new();
    internal SequenceTracing SequenceTracing
    {
        get;
    }
    internal RuntimeSystemProfile SystemProfiling
    {
        get;
    }
    internal ProcessTracing ProcessTracing
    {
        get;
    }
    private readonly Dictionary<ReferenceTerm, ProcessMonitorEntry> monitors = new();
    private readonly Dictionary<ReferenceTerm, ProcessAliasEntry> aliases = new();
    private readonly Dictionary<ReferenceTerm, ProcessSpawnRequestEntry> spawnRequests = new();
    private long nextPid, nextRef;
    private readonly object referenceGate = new();
    private uint referenceDomain;
    private bool disposed;
    private readonly RuntimeInitializer initializer;
    public Pid InitializationPid
    {
        get;
    }
    internal LocalNodeIdentity LocalIdentity
    {
        get;
    }
    public string ConfiguredNode
    {
        get;
    }
    public string Node => LocalIdentity.Snapshot.Node;
    public Atom NodeAtom => Term.A(LocalIdentity.Snapshot.Node);
    public TextWriter Output
    {
        get;
    }
    internal RuntimeStatistics Statistics { get; } = new();
    internal RuntimeTimers Timers
    {
        get;
    }
    internal RuntimeKnownNodes KnownNodes
    {
        get;
    }
    internal ProcessSuspensions Suspensions
    {
        get;
    }
    internal RuntimeSystemMonitor SystemMonitoring
    {
        get;
    }
    public RuntimeStandardStreams StandardStreams
    {
        get;
    }
    private readonly bool ownsStandardStreams;
    public int BacktraceDepth
    {
        get => Volatile.Read(ref backtraceDepth);
        internal set => Volatile.Write(ref backtraceDepth, value);
    }
    public ModuleRegistry Modules { get; } = new();
    public RuntimeFunctionEntries Functions { get; } = new();
    public RuntimeReferenceResources ReferenceResources
    {
        get;
    }
    internal uint LocalTermSeed { get; } = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(System.Security.Cryptography.RandomNumberGenerator.GetBytes(ErlangEtfParameters.LocalSeedBytes));

    public ProcessRuntime(string node = ProcessNodeDefaults.Local, TextWriter? output = null, int maximumProcesses = ProcessTableParameters.DefaultCapacity)
        : this(
            node,
            output,
            maximumProcesses,
            null
        )
    {
    }

    public ProcessRuntime(RuntimeStandardStreams standardStreams)
        : this(
            ProcessNodeDefaults.Local,
            null,
            ProcessTableParameters.DefaultCapacity,
            standardStreams
        )
    {
    }

    public ProcessRuntime(int maximumActorConcurrency)
        : this(
            ProcessNodeDefaults.Local,
            null,
            ProcessTableParameters.DefaultCapacity,
            null,
            maximumActorConcurrency
        )
    {
    }

    public ProcessRuntime(ProcessRuntimeOptions options)
        : this(
            ProcessNodeDefaults.Local,
            null,
            ProcessTableParameters.DefaultCapacity,
            null,
            Environment.ProcessorCount,
            options
        )
    {
    }

    public ProcessRuntime(
        string node,
        TextWriter? output,
        int maximumProcesses,
        RuntimeStandardStreams? standardStreams
    )
        : this(
            node,
            output,
            maximumProcesses,
            standardStreams,
            Environment.ProcessorCount
        )
    {
    }

    public ProcessRuntime(
        string node,
        TextWriter? output,
        int maximumProcesses,
        RuntimeStandardStreams? standardStreams,
        int maximumActorConcurrency
    )
        : this(
            node,
            output,
            maximumProcesses,
            standardStreams,
            maximumActorConcurrency,
            new ProcessRuntimeOptions()
        )
    {
    }

    public ProcessRuntime(
        string node,
        TextWriter? output,
        int maximumProcesses,
        RuntimeStandardStreams? standardStreams,
        int maximumActorConcurrency,
        ProcessRuntimeOptions options
    ) : this(
        node,
        output,
        maximumProcesses,
        standardStreams,
        maximumActorConcurrency,
        options,
        null
    )
    {
    }

    internal ProcessRuntime(
        string node,
        TextWriter? output,
        int maximumProcesses,
        RuntimeStandardStreams? standardStreams,
        int maximumActorConcurrency,
        ProcessRuntimeOptions options,
        RuntimeWorkerCreationPolicy? creation
    )
    {
        workerCreation = creation;
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.DirtyIoSchedulerCount, DirtyNativePoolParameters522.MinimumIoWorkers, nameof(options.DirtyIoSchedulerCount));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.DirtyIoSchedulerCount, DirtyNativePoolParameters522.MaximumIoWorkers, nameof(options.DirtyIoSchedulerCount));
        dirtyIoSchedulerCount522 = options.DirtyIoSchedulerCount;
        OwnedTermsPilot506 = options.OwnedTermsPilot506;
        NativeMetadataImage527 = options.NativeMetadataImage;
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaximumAtoms, AtomRuntimeParameters.MinimumCapacity);
        AtomNames = new(options.MaximumAtoms, AtomTableExhausted);
        using var atomScope = new AtomNameScope(AtomNames);
        WarningMapping = options.WarningMapping;
        PrintableRange = options.PrintableRange;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumActorConcurrency);
        processSlots = new(maximumProcesses);
        OsEnvironment = OperatingSystem.IsWindows() ? RuntimeOsEnvironment.CaptureWindows() : null;
        try
        {
            emptyTuple803 = new RuntimeEmptyTupleDomain803(this);
            workerCreation?.ConstructorCheckpoint(RuntimeWorkerCreationNames.BeforeDispatcher, this);
            actorDispatcher = new(
                maximumActorConcurrency,
                Statistics.UserCpu,
                Statistics.Microstates,
                true,
                workerCreation
            );
            workerCreation?.ConstructorCheckpoint(RuntimeWorkerCreationNames.BeforeAtoms, this);
            AtomNameRegistry.Observe(node);
            ConfiguredNode = node;
            LocalIdentity = new(ProcessNodeDefaults.Local);
            ReferenceResources = new(Modules, LocalIdentity);
            KnownNodes = new(LocalIdentity, TryGetDistributionEntryIdentity, AtomNames.Observe);
            Modules.AttachRuntime(this);
            Output = output ?? Console.Out;
            ownsStandardStreams = standardStreams is null;
            StandardStreams = standardStreams ?? RuntimeStandardStreams.Open();
            workerCreation?.ConstructorCheckpoint(RuntimeWorkerCreationNames.AfterStreams, this);
            Timers = new(this, gate);
            Suspensions = new(gate, processes, Timers);
            SystemMonitoring = new(this, gate, processes);
            Ports = new(this, gate);
            SequenceTracing = new(this, gate, processes);
            SystemProfiling = new(this, gate, processes);
            ProcessTracing = new(this, gate, processes);
            var initial = new ProcessContext(this, new Pid(LocalIdentity, InitializationNames.ProcessId))
            {
                IsSystemProcess = true
            };
            InitializationPid = initial.Self;
            processes.Add(initial.Self, initial);
            RegisterOwnedTermContext506(initial);
            processSlots.Add(initial.Self);
            initializer = new(initial);
            StartActualWorkersBeforeInitialization();
            workerCreation?.ConstructorCheckpoint(RuntimeWorkerCreationNames.BeforeInitializer, this);
            initializer.Start();
            workerCreation?.ConstructorCheckpoint(RuntimeWorkerCreationNames.BeforeClrRegistration, this);
            ClrMemorySources.Register(this);
        }
        catch (Exception startup)
        {
            Exception failure = RollbackInitializationWorkers(startup);
            if (ReferenceEquals(failure, startup))
                throw;
            throw failure;
        }
    }

    public ProcessHandle Spawn(Func<ProcessContext, ValueTask<Term>> body, ProcessContext? parent = null, bool link = false)
    {
        return SpawnTraced(
            body,
            parent,
            link,
            null
        );
    }

    internal ProcessHandle SpawnTraced(
        Func<ProcessContext, ValueTask<Term>> body,
        ProcessContext? parent,
        bool link,
        SequenceSpawnDescription? description,
        ReferenceTerm? spawnReference = null,
        bool asynchronousSpawn = false,
        bool successSpawnReply = true,
        bool initialAsyncDistribution = false,
        ProcessSchedulingPriority initialPriority = ProcessSchedulingPriority.Normal,
        int schedulerBinding = ProcessSchedulerNames.Unbound,
        ushort? initialFullSweepAfter = null,
        bool systemProcess = false,
        ProcessMessageQueueDataMode411 initialMessageQueueData = ProcessMessageQueueData411.Default,
        PreparedLocalSpawnTrace641? preparedTrace641 = null
    )
    {
        using var spawnInput641 = RuntimeVmSpawnInput641.PrepareBody641(this, body, !System.Threading.Monitor.IsEntered(gate));
        ProcessContext ctx;
        while (true)
        {
            using var directTrace641 = parent is not null && preparedTrace641 is null
                ? SequenceTracing.PrepareLocalSpawnTrace641(
                    parent,
                    description,
                    asynchronousSpawn,
                    successSpawnReply
                ) : null;
            PreparedLocalSpawnTrace641? trace641 = preparedTrace641 ?? directTrace641;
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (link && parent is not null)
                    RequireAlive(parent.Self);
                if (parent is not null && !trace641!.IsCurrent())
                {
                    if (preparedTrace641 is not null)
                        throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
                    continue;
                }
                processSlots.RequireCapacity();
                ctx = new(
                    this,
                    new Pid(LocalIdentity, (ulong)++nextPid),
                    description?.Mfa
                )
                {
                    IsSystemProcess = systemProcess
                };
                ctx.GarbageCollectionPolicy.ExchangeThreshold(initialFullSweepAfter ?? DefaultFullSweepAfter);
                ctx.AsyncDistribution = initialAsyncDistribution;
                ctx.Mailbox.ExchangeMessageQueueData411(initialMessageQueueData);
                ctx.SchedulingPriorityLevel = initialPriority;
                ctx.SchedulerBinding = schedulerBinding;
                ctx.GroupLeader = parent?.GroupLeader ?? processes[InitializationPid].GroupLeader;
                processes.Add(ctx.Self, ctx);
                executingProcesses.Add(ctx);
                processSlots.Add(ctx.Self);
                bool enqueued = false;
                try
                {
                    RegisterOwnedTermContext506(ctx);
                    if (body.Target is RuntimeVmSpawnInput641 input641) input641.Claim641(ctx);
                    _ = actorDispatcher.Dispatch(
                        ctx,
                        async () =>
                    {
                        lock (gate)
                            ctx.ExecutionStarted = true;
                        Term reason = Term.A(ProcessExitReasons.Normal);
                        RuntimeProcessPayload641? reason641 = null;
                        ProcessHibernateTransfer? waitingHibernate641 = null;
                        Exception? sourceTeardown641 = null;
                        try
                        {
                            MaterializePendingSequenceTraceToken(ctx);
                            ThrowIfHalted();
                            ctx.Cancellation.ThrowIfCancellationRequested();
                            Func<ProcessContext, ValueTask<Term>> current = body;
                            body = null!;
                            while (true)
                            {
                                try
                                {
                                    await current(ctx);
                                    break;
                                }
                                catch (ProcessHibernateTransfer transfer)
                                {
                                    waitingHibernate641?.Origin641?.Close641();
                                    waitingHibernate641 = transfer;
                                    if (transfer.Teardown641 is { } teardown641)
                                    {
                                        sourceTeardown641 = teardown641;
                                        throw teardown641;
                                    }
                                    current = child => child.Runtime.Modules.CallVmMfaRoot641(
                                        child,
                                        transfer.Module,
                                        transfer.Function,
                                        transfer.Arguments,
                                        transfer.Origin641
                                    );
                                    await ctx.Mailbox.WaitForMessage(ctx.Cancellation);
                                }
                            }
                        }
                        catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, this))
                        {
                            return;
                        }
                        catch (OperationCanceledException) when (IsHaltRequested)
                        {
                            return;
                        }
                        catch (ReOwnerClosing411 closing) when (ReferenceEquals(closing.Owner, this))
                        {
                            reason = Term.A(ProcessExitReasons.Shutdown);
                        }
                        catch (ModuleSourceMutationClosedException closing) when (ReferenceEquals(closing.Registry, Modules))
                        {
                            reason = Term.A(ProcessExitReasons.Shutdown);
                        }
                        catch (OperationCanceledException) when (ctx.Cancellation.IsCancellationRequested)
                        {
                            return;
                        }
                        catch (ProcessVmFailure641 failure641) when (
                            ReferenceEquals(failure641.Runtime641, this) && failure641.Process641.Equals(ctx.Self))
                        {
                            reason641 = failure641.Origin641;
                            reason = reason641.Value641;
                            sourceTeardown641 = failure641.Teardown641;
                        }
                        catch (ErlangException ex)
                        {
                            reason = ex.ExceptionClass == ErlangExceptionClasses.Exit ? ex.Reason : Term.Tuple(
                                ex.ExceptionClass == ErlangExceptionClasses.Throw ? Term.Tuple(Term.A(ErlangErrorReasons.NoCatch), ex.Reason) : ex.Reason,
                                ex.StackTraceTerm
                            );
                            // Existing opaque Host transport remains a Host boundary.
                            // Capture a detached process-owned reason while its genuine producer is alive.
                            try
                            {
                                reason641 = RuntimeProcessPayload641.Prepare641(this, ctx, reason);
                                reason = reason641.Value641;
                            }
                            catch (Exception preparation641)
                            {
                                sourceTeardown641 = preparation641;
                                reason = Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(preparation641.GetType().Name));
                            }
                        }
                        catch (Exception ex)
                        {
                            reason = Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(ex.GetType().Name));
                        }
                        finally
                        {
                            try
                            {
                                TerminateWithSequencePreparation(
                                    ctx,
                                    reason,
                                    reason641,
                                    sourceTeardown641
                                );
                            }
                            finally
                            {
                                waitingHibernate641?.Origin641?.Close641();
                                reason641?.Close641();
                                ctx.VmSpawnInput641?.CloseClaimed641();
                                lock (gate)
                                    executingProcesses.Remove(ctx);
                                try
                                {
                                    ctx.BeginOwnedTermRetirement506();
                                }
                                finally
                                {
                                    ctx.Unwound.TrySetResult();
                                }
                            }
                        }
                    }
                    );
                    enqueued = true;
                    if (link && parent is not null)
                    {
                        EstablishSpawnLink(ctx, parent);
                    }
                    if (parent is not null)
                        SequenceTracing.SpawnedPrepared(
                            parent,
                            ctx,
                            trace641!,
                            spawnReference
                        );
                }
                catch
                {
                    if (!enqueued) ctx.VmSpawnInput641?.CloseClaimed641();
                    RollbackActorStart(ctx, enqueued);
                    throw;
                }
            }

            break;
        }

        return new(ctx.Self, ctx.Done.Task);
    }

    internal Term SpawnWithOptions(
        ProcessContext parent,
        Func<ProcessContext, ValueTask<Term>> body,
        ProcessSpawnOptions options,
        SequenceSpawnDescription? description = null
    )
    {
        using var spawnInput641 = RuntimeVmSpawnInput641.PrepareBody641(this, body, !System.Threading.Monitor.IsEntered(gate));
        var input641 = PrepareMonitorInput641(parent, Nil.Value, options.MonitorOptions);
        try
        {
            ProcessMonitorOptions monitorOptions641 = PreparedMonitorOptions641(options.MonitorOptions, input641);
            while (true)
            {
                lock (gate) RequireAlive(parent.Self);
                using var trace641 = SequenceTracing.PrepareLocalSpawnTrace641(
                    parent,
                    description,
                    false,
                    true
                );
                lock (gate)
                {
                    RequireAlive(parent.Self);
                    if (trace641 is not null && !trace641.IsCurrent())
                        continue;
                    ReferenceTerm? reference = options.Monitor ? ReserveMonitorReference(parent, monitorOptions641) : null;
                    var process = SpawnTraced(
                        body,
                        parent,
                        options.Link,
                        description,
                        reference,
                        initialAsyncDistribution: options.AsyncDistribution,
                        initialPriority: options.Priority,
                        schedulerBinding: options.ResolveScheduler(ActorDispatchCapacity),
                        initialFullSweepAfter: options.FullSweepAfter,
                        initialMessageQueueData: options.MessageQueueData,
                        preparedTrace641: trace641
                    );
                    if (options.PriorityLink)
                        parent.PriorityLinks.Add(process.Pid);
                    if (options.Monitor)
                        return Term.Tuple(process.Pid, Monitor(
                            parent,
                            process.Pid,
                            monitorOptions641,
                            reference
                        ));

                    return process.Pid;
                }
            }

        }
        finally
        {
            input641.Close641();
        }
    }

    public (ProcessHandle Process, ReferenceTerm Reference) SpawnMonitor(ProcessContext observer, Func<ProcessContext, ValueTask<Term>> body, bool link = false)
    {
        return SpawnMonitorTraced(
            observer,
            body,
            link,
            null
        );
    }

    internal (ProcessHandle Process, ReferenceTerm Reference) SpawnMonitorTraced(
        ProcessContext observer,
        Func<ProcessContext, ValueTask<Term>> body,
        bool link,
        SequenceSpawnDescription? description
    )
    {
        using var spawnInput641 = RuntimeVmSpawnInput641.PrepareBody641(this, body, !System.Threading.Monitor.IsEntered(gate));
        var input641 = PrepareMonitorInput641(observer, Nil.Value, ProcessMonitorOptions.Default);
        try
        {
            ProcessMonitorOptions monitorOptions641 = PreparedMonitorOptions641(ProcessMonitorOptions.Default, input641);
            while (true)
            {
                lock (gate) RequireAlive(observer.Self);
                using var trace641 = SequenceTracing.PrepareLocalSpawnTrace641(
                    observer,
                    description,
                    false,
                    true
                );
                lock (gate)
                {
                    RequireAlive(observer.Self);
                    if (trace641 is not null && !trace641.IsCurrent())
                        continue;
                    ReferenceTerm reference = ReserveMonitorReference(observer, monitorOptions641);
                    var process = SpawnTraced(
                        body,
                        observer,
                        link,
                        description,
                        reference,
                        preparedTrace641: trace641
                    );

                    return (process, Monitor(
                        observer,
                        process.Pid,
                        monitorOptions641,
                        reference
                    ));
                }
            }

        }
        finally
        {
            input641.Close641();
        }
    }

    internal ReferenceTerm SpawnRequest(
        ProcessContext owner,
        Func<ProcessContext, ValueTask<Term>> body,
        ProcessSpawnRequestOptions options,
        Term? failure,
        SequenceSpawnDescription description
    )
    {
        using var spawnInput641 = RuntimeVmSpawnInput641.PrepareBody641(this, body, !System.Threading.Monitor.IsEntered(gate));
        var input641 = PrepareMonitorInput641(owner, Nil.Value, options.Spawn.MonitorOptions);
        try
        {
            ProcessMonitorOptions monitorOptions641 = PreparedMonitorOptions641(options.Spawn.MonitorOptions, input641);
            while (true)
            {
                lock (gate) RequireAlive(owner.Self);
                using var trace641 = failure is null ? SequenceTracing.PrepareLocalSpawnTrace641(
                    owner,
                    description,
                    true,
                    options.SuccessReply
                ) : null;
                lock (gate)
                {
                    RequireAlive(owner.Self);
                    if (trace641 is not null && !trace641.IsCurrent())
                        continue;
                    ReferenceTerm ordinaryReference = MakeReference();
                    ReferenceTerm reference = ordinaryReference;
                    ProcessHandle? child = null;
                    if (failure is null)
                    {
                        try
                        {
                            processSlots.RequireCapacity();
                            if (options.Spawn.Monitor && monitorOptions641.AliasMode is not null)
                            {
                                ulong ownerPid = (owner.Self.Id << ProcessAliasLayout.PidTagBits) | ProcessAliasLayout.PidTag;
                                reference = ReferenceTerm.CreateLocalPid775(
                                    LocalIdentity,
                                    [reference.Words[0], reference.Words[1] | ProcessAliasLayout.PidMarker, reference.Words[2], (uint)ownerPid, (uint)(ownerPid >> ReferenceIdentityLayout.WordBits)]
                                );
                            }
                            child = SpawnTraced(
                                body,
                                owner,
                                options.Spawn.Link,
                                description,
                                reference,
                                true,
                                options.SuccessReply,
                                options.Spawn.AsyncDistribution,
                                options.Spawn.Priority,
                                options.Spawn.ResolveScheduler(ActorDispatchCapacity),
                                options.Spawn.FullSweepAfter,
                                initialMessageQueueData: options.Spawn.MessageQueueData,
                                preparedTrace641: trace641
                            );
                        }
                        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.SystemLimit)))
                        {
                            failure = exception.Reason;
                            reference = ordinaryReference;
                        }
                    }
                    if (child is null)
                    {
                        if (options.ErrorReply)
                            owner.Mailbox.SendTraced(
                                Term.Tuple(
                                    options.Tag,
                                    reference,
                                    Term.A(ProcessSpawnRequestNames.Error),
                                    failure!
                                ),
                                null,
                                preserve: true,
                                from: NodeAtom
                            );

                        return reference;
                    }
                    if (options.Spawn.Monitor)
                    {
                        if (monitorOptions641.AliasMode is not null)
                            aliases.Add(reference, new(owner.Self, monitorOptions641.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false));
                        Monitor(
                            owner,
                            child.Pid,
                            monitorOptions641,
                            reference
                        );
                    }
                    SequenceTraceToken? replyToken = processes[child.Pid].SequentialTrace.Token;
                    if (options.Spawn.PriorityLink)
                        owner.PriorityLinks.Add(child.Pid);
                    spawnRequests.Add(reference, new(owner.Self, child.Pid, options));
                    owner.Mailbox.SendPending(
                        Term.Tuple(
                            options.Tag,
                            reference,
                            Term.A(ProcessSpawnRequestNames.Success),
                            child.Pid
                        ),
                        () =>
                        {
                            if (!spawnRequests.Remove(reference, out var request))
                                return null;

                            return request.Options.SuccessReply ? request.Options.Spawn.MonitorOptions.Priority : null;
                        },
                        replyToken,
                        from: child.Pid
                    );

                    return reference;
                }
            }

        }
        finally
        {
            input641.Close641();
        }
    }

    internal bool AbandonSpawnRequest(ProcessContext owner, ReferenceTerm reference)
    {
        lock (gate)
        {
            RequireAlive(owner.Self);
            if (!spawnRequests.TryGetValue(reference, out var request) || !request.Owner.Equals(owner.Self))
                return distributionPending?.AbandonConnectedSpawnRequest(owner, reference) ?? false;
            spawnRequests.Remove(reference);
            RemoveMonitor641(reference);
            aliases.Remove(reference);
            if (request.Options.Spawn.Link)
            {
                owner.Links.Remove(request.Child);
                owner.LinkIdentities.Remove(request.Child);
                owner.PriorityLinks.Remove(request.Child);
                if (processes.TryGetValue(request.Child, out var child))
                {
                    child.Links.Remove(owner.Self);
                    child.LinkIdentities.Remove(owner.Self);
                    child.PriorityLinks.Remove(owner.Self);
                    QueueExit(
                        child,
                        owner.Self,
                        Term.A(ProcessSpawnRequestNames.Abandoned),
                        false,
                        false,
                        false,
                        null,
                        false
                    );
                }
            }

            return true;
        }
    }

    public Term Send(Pid target, Term message)
    {
        ObserveIncomingTerm(message);
        if (Volatile.Read(ref haltResult) is not null)
            throw new ObjectDisposedException(nameof(ProcessRuntime));
        if (TrySendBackedLocalAsync(target, message).GetAwaiter().GetResult()) return message;
        lock (gate)
        {
            if (processes.TryGetValue(target, out var p))
                p.Mailbox.Send(message);
        }

        return message;
    }

    internal void SendSystem(Pid target, Term message, Term? from = null)
    {
        lock (gate)
        {
            if (processes.TryGetValue(target, out var receiver))
                receiver.Mailbox.SendTraced(
                    message,
                    null,
                    preserve: true,
                    from: from
                );
        }
    }

    internal Term SendTraced(
        ProcessContext sender,
        Pid target,
        Term message,
        Term? destination = null
    )
    {
        if (TrySendBackedLocalAsync(target, message, sender).GetAwaiter().GetResult()) return message;
        lock (gate)
        {
            ProcessTracing.Sent(
                sender,
                destination ?? target,
                message,
                processes.ContainsKey(target)
            );
            if (processes.TryGetValue(target, out var receiver))
            {
                SequenceTraceToken? token = SequenceTracing.Sent(sender, target, message);
                receiver.Mailbox.SendTraced(message, token, from: sender.Self);
            }
        }

        return message;
    }

    public ReferenceTerm CreateAlias(ProcessContext owner, bool reply, bool priority)
    {
        lock (gate)
        {
            RequireAlive(owner.Self);
            ReferenceTerm ordinary = MakeReference();
            ulong pid = (owner.Self.Id << ProcessAliasLayout.PidTagBits) | ProcessAliasLayout.PidTag;
            var reference = ReferenceTerm.CreateLocalPid775(
                LocalIdentity,
                [ordinary.Words[0], ordinary.Words[1] | ProcessAliasLayout.PidMarker, ordinary.Words[2], (uint)pid, (uint)(pid >> ReferenceIdentityLayout.WordBits)]
            );
            aliases.Add(reference, new(owner.Self, reply, priority));

            return reference;
        }
    }

    public bool Unalias(ProcessContext owner, ReferenceTerm reference)
    {
        lock (gate)
        {
            if (!aliases.TryGetValue(reference, out var alias) || !alias.Owner.Equals(owner.Self))
                return false;

            return aliases.Remove(reference);
        }
    }

    public Term Send(ReferenceTerm reference, Term message, bool priority = false)
    {
        ObserveIncomingTerm(message);

        return SendAlias(
            reference,
            message,
            priority,
            null
        );
    }

    internal Term SendAlias(
        ReferenceTerm reference,
        Term message,
        bool priority,
        ProcessContext? sender
    )
    {
        if (TrySendBackedAliasAsync(
            reference,
            message,
            priority,
            sender
        ).GetAwaiter().GetResult()) return message;
        lock (gate)
        {
            Pid? traceOwner = ResolveAliasOwner(reference);
            if (sender is not null)
                ProcessTracing.Sent(
                    sender,
                    reference,
                    message,
                    true
                );
            SequenceTraceToken? token = sender is null || traceOwner is null || !processes.ContainsKey(traceOwner) ? null : SequenceTracing.Sent(sender, reference, message);
            if (aliases.TryGetValue(reference, out var alias) && processes.TryGetValue(alias.Owner, out var owner))
            {
                owner.Mailbox.SendPending(
                    message,
                    () =>
                {
                    if (!aliases.TryGetValue(reference, out var current))
                        return null;
                    if (current.Reply)
                    {
                        aliases.Remove(reference);
                        RemoveMonitor641(reference);
                        DetachConnectedSpawnMonitor(owner, reference);
                    }

                    return priority && current.Priority;
                },
                    token,
                    from: sender?.Self
                );
            }
        }

        return message;
    }

    private Pid? ResolveAliasOwner(ReferenceTerm reference)
    {
        if (aliases.TryGetValue(reference, out var alias))
            return alias.Owner;
        if (reference.Node != Node || reference.Words.Count <= ProcessAliasLayout.OwnerHighWord || (reference.Words[1] & ProcessAliasLayout.PidMarker) == 0)
            return null;
        ulong encoded = reference.Words[ProcessAliasLayout.OwnerLowWord] | ((ulong)reference.Words[ProcessAliasLayout.OwnerHighWord] << ReferenceIdentityLayout.WordBits);

        return new Pid(LocalIdentity, encoded >> ProcessAliasLayout.PidTagBits);
    }

    private void AdmitSignalsCore(ProcessContext owner)
    {
        using var signalOwner428 = TryBeginSignalOwner428(owner);
        if (signalOwner428 is null) return;
        using var outputTransfer = distributionPending?.BeginOutputTransfer();
        if (TryAdmitReadonlyPrefix817(owner)) return;
        if (owner.DeferredSignalDrain is { } drain && (drain.IsCurrent || owner.Mailbox.HasDeferredPending || owner.Mailbox.HasPendingBacking))
        {
            if (drain.IsCurrent)
                return;
            // Existing locked HOST callbacks may read the admitted prefix, never decode.
            if (System.Threading.Monitor.IsEntered(gate) || owner.Mailbox.DeferredGateHeld)
            {
                lock (gate)
                    owner.Mailbox.AdmitPending();

                return;
            }
            drain.Run(() => DrainDeferredProcessSignals(owner));

            return;
        }

        lock (gate)
        {
            if (owner.Terminated)
                owner.Mailbox.DiscardPending();
            else
                owner.Mailbox.AdmitPending();
        }
    }

    public bool IsAlive(Pid pid)
    {
        lock (gate)
            return processes.ContainsKey(pid);
    }

    public Pid GetGroupLeader(ProcessContext owner)
    {
        lock (gate)
            return RequireAlive(owner.Self).SnapshotGroupLeader();
    }

    internal Pid GetGuestGroupLeader(ProcessContext owner)
    {
        lock (gate)
        {
            ProcessContext alive = RequireAlive(owner.Self);

            return owner.CurrentOwned506 is { CanBorrow507: true } invocation
                && invocation.OwnsContext507(owner) && RuntimeActorExecutionTurn.IsOwnedBy(owner)
                ? alive.GroupLeader : alive.SnapshotGroupLeader();
        }
    }

    public bool SetGroupLeader(Pid leader, Pid target)
    {
        lock (gate)
        {
            if (target.Node != Node)
                return true;
            if (!processes.TryGetValue(target, out var process))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            process.GroupLeader = leader;

            return true;
        }
    }

    public IReadOnlyList<Pid> ProcessSnapshot()
    {
        lock (gate)
            return Array.AsReadOnly(processes.Keys.ToArray());
    }

    internal ProcessTableBatch? ReadProcessBatch(int cursor)
    {
        lock (gate)
            return processSlots.Read(cursor);
    }

    internal Term ProcessInfo(Pid pid, IReadOnlyList<ProcessInfoItem> keys, bool singleton)
        => CaptureProcessInfoSnapshot601(pid, keys, singleton).CompleteAndDispose(this, null);

    private ProcessInfoSnapshot601 CaptureProcessInfoSnapshot601(Pid pid, IReadOnlyList<ProcessInfoItem> keys, bool singleton)
    {
        var captured = new ProcessInfoSnapshot601();
        FillProcessInfoSnapshot601(
            captured,
            pid,
            keys,
            singleton
        );

        return captured;
    }

    private void FillProcessInfoSnapshot601(
        ProcessInfoSnapshot601 captured,
        Pid pid,
        IReadOnlyList<ProcessInfoItem> keys,
        bool singleton,
        Term? binary823 = null
    )
    {
        try
        {
            captured.Value = ReadProcessInfoSnapshot601(
                pid,
                keys,
                singleton,
                captured,
                binary823
            );
        }
        catch (Exception error)
        {
            captured.Failure = error;
        }
    }

    private Term ReadProcessInfoSnapshot601(
        Pid pid,
        IReadOnlyList<ProcessInfoItem> keys,
        bool singleton,
        ProcessInfoSnapshot601 captured,
        Term? binary823
    )
    {
        ProcessContext observed;
        lock (gate)
        {
            if (!processes.TryGetValue(pid, out var found))
                return ProcessInfoAtoms.Undefined;
            observed = found;
        }
        if (keys.Any(key => key.Name == ProcessInfoNames.Links || key.RequiresOwnerSnapshot || key.Name == ProcessInfoNames.MessageQueueLength))
            AdmitSignals(observed);
        if (binary823 is null && keys.Any(key => key.Name == ProcessBinaryInfo823Names.Binary))
        {
            binary823 = CaptureLiveBinaryInfo823(observed);
            if (binary823 is null) return ProcessInfoAtoms.Undefined;
        }
        lock (gate)
        {
            if (!processes.TryGetValue(pid, out var process) || !ReferenceEquals(process, observed) || process.Terminated)
                return ProcessInfoAtoms.Undefined;
            var values = new List<Term>(keys.Count);
            IReadOnlyList<Term>? messages = keys.Any(key => key.Name is ProcessInfoNames.Messages or ProcessInfoNames.MessageQueueLength)
                ? (captured.Messages = process.Mailbox.CaptureSnapshot601()).Values.ToArray() : null;
            foreach (ProcessInfoItem key in keys)
            {
                Term value = key.Name switch
                {
                    ProcessBinaryInfo823Names.Binary => binary823 ?? throw new InvalidOperationException(ProcessBinaryInfo823Names.InvalidAdmission),
                    ProcessInfoLogicalNames.Suspending => Suspensions.SnapshotOwned(process.Self),
                    ProcessInfoLogicalNames.InitialCall => process.InitialCall,
                    ProcessInfoLogicalNames.ErrorHandler => process.ErrorHandler,
                    ProcessInfoLogicalNames.Priority => process.SchedulingPriority,
                    ProcessFullSweepNames.Flag => Term.I(process.GarbageCollectionPolicy.Threshold),
                    ProcessAsyncDistributionNames.Flag => CoreModules.Boolean(process.AsyncDistribution),
                    ProcessMessageQueueDataNames411.Flag => ProcessMessageQueueData411.Atom(process.Mailbox.MessageQueueData411),
                    ProcessInfoLogicalNames.Reductions => Term.I(process.CumulativeReductions),
                    ProcessInfoNames.Links => Cons.From(process.Links.Cast<Term>().Concat(process.PortLinks)),
                    ProcessInfoNames.PriorityMessages => Term.A(PriorityPermissionCount(process) != 0 ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                    ProcessInfoNames.TrapExit => Term.A(process.TrapExits ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                    ProcessInfoNames.GroupLeader => process.SnapshotGroupLeader(),
                    ProcessInfoNames.LastCalls => process.SavedCalls.Snapshot(),
                    ProcessInfoNames.Dictionary when key.DictionaryKey is { } dictionaryKey => process.Dictionary.ValueForProcessInfo611(captured, dictionaryKey),
                    ProcessInfoNames.Dictionary => process.Dictionary.SnapshotForProcessInfo611(captured),
                    ProcessInfoNames.Messages => Cons.From(messages!),
                    ProcessInfoNames.MessageQueueLength => Term.I(messages!.Count),
                    ProcessInfoNames.Monitors => SnapshotOriginMonitors(process.Self),
                    ProcessInfoNames.MonitoredBy => Cons.From(monitors.Values.Where(monitor => monitor.Target?.Equals(process.Self) == true).Select(monitor => (Term)monitor.Observer)),
                    ProcessInfoNames.Status => Term.A(process.SuspensionRequested ? ProcessInfoStatusNames.Suspended : process.WaitingForMessage ? ProcessInfoStatusNames.Waiting :
                        !process.ExecutionStarted || process.Yielding ? ProcessInfoStatusNames.Runnable : ProcessInfoStatusNames.Running),
                    ProcessInfoNames.CurrentFunction => process.SnapshotCurrentFunction(),
                    ProcessInfoNames.CurrentStackTrace => process.SnapshotCurrentStackTrace(),
                    ProcessInfoNames.RegisteredName => names.FirstOrDefault(pair => pair.Value.Equals(pid)).Key is string name ? Term.A(name) : Nil.Value,
                    _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                };
                if (singleton && key.Name == ProcessInfoNames.RegisteredName && value is Nil)
                    return Nil.Value;
                values.Add(Term.Tuple(key.Key, value));
            }

            return singleton ? values[0] : Cons.From(values);
        }
    }

    public IReadOnlyList<string> RegisteredSnapshot()
    {
        lock (gate)
            return Array.AsReadOnly(names.Keys.ToArray());
    }

    public ProcessHandle GetProcessHandle(Pid pid)
    {
        lock (gate)
        {
            var process = RequireAlive(pid);

            return new(pid, process.Done.Task);
        }
    }

    public void Register(string name, Pid pid)
    {
        lock (gate)
        {
            if (!IsCurrentLocal(pid))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            RequireAlive(pid);
            if (name == ProcessRegistryAtoms.Undefined || names.ContainsKey(name) || names.ContainsValue(pid))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            names.Add(name, pid);
        }
    }

    public bool TryRegisterStartupName(string name, Pid pid, out Pid? existing)
    {
        lock (gate)
        {
            if (!IsCurrentLocal(pid))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            RequireAlive(pid);
            if (name == ProcessRegistryAtoms.Undefined || names.ContainsValue(pid))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (names.TryGetValue(name, out var registered))
            {
                existing = registered as Pid;

                return false;
            }
            existing = null;
            names.Add(name, pid);

            return true;
        }
    }

    public Term WhereIs(string name)
    {
        lock (gate)
            return names.TryGetValue(name, out var p) ? p : Term.A(ProcessRegistryAtoms.Undefined);
    }

    public void Unregister(string name)
    {
        lock (gate)
        {
            if (!names.Remove(name))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private ProcessContext RequireAlive(Pid pid) => processes.TryGetValue(pid, out var p) ? p : throw new ErlangException(ErlangErrorReasons.NoProcess);

    internal async ValueTask<Term> ChangeSavedCalls(
        ProcessContext requester,
        Pid target,
        Term flag,
        Term value
    )
    {
        Term started = RequestFlag425(requester, new Term[] { target, flag, value });
        if (started is not ReferenceTerm reference) return started;
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(reference) ? tuple : null);

        return reply!.Items[1];
    }

    internal Term RequestFlag425(ProcessContext requester, IReadOnlyList<Term> arguments)
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        if (arguments[0] is not Pid target || !IsCurrentLocal(target)) return Term.A(ProcessSavedCallNames.BadType);
        Term flag = arguments[1];
        Term value = arguments[2];
        if (target.Equals(requester.Self))
            return ApplySavedCallFlag(requester, flag, value);
        var id = MakeReference();
        var request = new ProcessSavedCallRequest(
            requester,
            id,
            flag,
            value
        );
        lock (gate)
        {
            if (!processes.TryGetValue(target, out var process))
                return Term.A(ProcessSavedCallNames.BadType);
            process.PendingSavedCallRequests.Add(request);
            process.Mailbox.SendPending(
                Nil.Value,
                () =>
                {
                    lock (gate)
                    {
                        if (process.PendingSavedCallRequests.Remove(request))
                            ReplySavedCallRequest(request, ApplySavedCallFlag(process, request.Flag, request.Value), process.Self);
                    }

                    return null;
                }
            );
        }

        return id;
    }

    private static Term ApplySavedCallFlag(ProcessContext process, Term flag, Term value)
    {
        if (flag is not Atom { Name: ProcessSavedCallNames.Flag })
            return Term.A(ErlangErrorReasons.BadArgument);
        try
        {
            return Term.I(process.SavedCalls.Configure(value));
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return exception.Reason;
        }
    }

    private void ReplySavedCallRequest(ProcessSavedCallRequest request, Term result, Term? from = null) => SendSystem(request.Requester.Self, Term.Tuple(request.Id, result), from);

    internal async ValueTask<bool> PurgeModule(ProcessContext requester, string module)
    {
        var reference = MakeReference();
        Spawn(async worker =>
        {
            Term result;
            try
            {
                var admission = codePurger.WaitAsync(worker.Cancellation);
                try
                {
                    await worker.Mailbox.WaitForControlCompletion(admission, worker.Cancellation);
                    result = Term.A(await PurgeOldCode(worker, module) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
                }
                finally
                {
                    try
                    {
                        await admission;
                        codePurger.Release();
                    }
                    catch (OperationCanceledException) when (worker.Cancellation.IsCancellationRequested)
                    {
                    }
                }
            }
            catch (ProcessHaltTransfer transfer) when (ReferenceEquals(transfer.Runtime, this))
            {
                throw;
            }
            catch (ModuleSourceMutationClosedException closing) when (ReferenceEquals(closing.Registry, Modules))
            {
                throw;
            }
            catch (ErlangException exception)
            {
                result = Term.Tuple(Term.A(ErlangExceptionClasses.Error), exception.Reason);
            }
            catch (OperationCanceledException) when (worker.Cancellation.IsCancellationRequested)
            {
                result = Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(ErlangErrorReasons.NoProcess));
            }
            catch (Exception exception)
            {
                result = Term.Tuple(
                    Term.A(ErlangExceptionClasses.Error),
                    Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(exception.GetType().Name))
                );
            }
            Send(requester.Self, Term.Tuple(Term.A(CodePurgeNames.ReplyTag), reference, result));

            return Term.A(ErlangBooleanAtoms.True);
        });
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple &&
            tuple.Items[0] is Atom { Name: CodePurgeNames.ReplyTag } && tuple.Items[1].Equals(reference) ? tuple : null);
        if (reply!.Items[2] is TupleTerm failure)
            throw new ErlangException(failure.Items[1]);

        return reply.Items[2].Equals(Term.A(ErlangBooleanAtoms.True));
    }

    private async ValueTask<bool> PurgeOldCode(ProcessContext worker, string module)
    {
        SourcePurgeFenceSession? session = await Modules.PreparePurgeAsync(worker, module);
        if (session is null)
            return false;
        bool completed = false;
        try
        {
            await PurgeCodeImageProcesses(worker, session.Image);
            bool purged = await Modules.FinishPurgeAsync(session, true);
            completed = true;

            return purged;
        }
        finally
        {
            if (!completed)
                await Modules.FinishPurgeAsync(session, false);
        }
    }

    public void Link(ProcessContext source, Pid target, bool priority = false)
    {
        lock (gate)
        {
            RequireAlive(source.Self);
            if (source.Self.Equals(target))
                return;
            if (processes.TryGetValue(target, out var process))
            {
                EstablishLink(source, process);
                if (priority)
                    source.PriorityLinks.Add(target);
                else
                    source.PriorityLinks.Remove(target);

                return;
            }
            if (!source.TrapExits && target.Node == Node)
                throw new ErlangException(ErlangErrorReasons.NoProcess);
            bool activated = ActivateCallerLink(source, target);
            if (priority)
                source.PriorityLinks.Add(target);
            else
                source.PriorityLinks.Remove(target);
            if (!activated)
                return;
            QueueExit(
                source,
                target,
                Term.A(target.Node == Node ? ErlangErrorReasons.NoProcess : ProcessLinkFailureReasons.Disconnected),
                false,
                false,
                false,
                null,
                true
            );
        }
    }

    public void Unlink(ProcessContext source, Pid target)
    {
        lock (gate)
        {
            source.Links.Remove(target);
            source.PriorityLinks.Remove(target);
            if (!source.LinkIdentities.TryGetValue(target, out var identity) || identity.PendingUnlink is not null)
                return;
            if (!processes.TryGetValue(target, out var process))
            {
                source.LinkIdentities.Remove(target);

                return;
            }
            var unlink = new ProcessUnlinkIdentity();
            identity.PendingUnlink = unlink;
            process.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                if (process.LinkIdentities.TryGetValue(source.Self, out var current) && current.PendingUnlink is null)
                {
                    process.LinkIdentities.Remove(source.Self);
                    process.Links.Remove(source.Self);
                    process.PriorityLinks.Remove(source.Self);
                }
                if (!source.Terminated)
                {
                    source.Mailbox.SendPending(
                        Nil.Value,
                        () =>
                    {
                        if (source.LinkIdentities.TryGetValue(target, out var acknowledged) && ReferenceEquals(acknowledged.PendingUnlink, unlink))
                            source.LinkIdentities.Remove(target);

                        return null;
                    }
                    );
                }

                return null;
            }
            );
        }
    }

    public ReferenceTerm MakeReference()
    {
        ulong value;
        uint domain;
        lock (referenceGate)
        {
            nextRef = unchecked(nextRef + 1);
            value = unchecked((ulong)nextRef);
            if (value == 0)
            {
                referenceDomain = referenceDomain == (uint)ReferenceDomainCapacity
                    ? ReferenceNumberLayout.HostDomain
                    : referenceDomain + 1;
            }
            domain = referenceDomain;
        }

        return ReferenceTerm.CreateOrdinaryLocal562(
            LocalIdentity,
            [
            (uint)(value & ReferenceIdentityLayout.LocalFirstWordMask),
            (uint)value & ReferenceNumberLayout.CountMiddleMask | domain,
            (uint)(value >> ReferenceNumberLayout.CountHighShift)
        ]
        );
    }

    public ReferenceTerm Monitor(ProcessContext observer, Pid target) => MonitorSource(
        observer,
        ProcessMonitorKinds.Process,
        target,
        ProcessMonitorOptions.Default
    );

    private ReferenceTerm ReserveMonitorReference(ProcessContext observer, ProcessMonitorOptions options)
    {
        return options.AliasMode is null ? MakeReference() : CreateAlias(observer, options.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false);
    }

    internal ReferenceTerm Monitor(
        ProcessContext observer,
        Term target,
        ProcessMonitorOptions options,
        ReferenceTerm? reservedReference = null
    )
    {
        lock (gate)
        {
            RequireAlive(observer.Self);
            Pid? recipient;
            Term reported;
            if (target is Pid pid)
            {
                recipient = pid;
                reported = pid;
            }
            else if (target is Atom name)
            {
                recipient = WhereIs(name.Name) as Pid;
                reported = Term.Tuple(name, NodeAtom);
            }
            else if (target is TupleTerm { Items.Count: 2 } address && address.Items[0] is Atom named && address.Items[1] is Atom { Name: var node } && node == Node)
            {
                recipient = WhereIs(named.Name) as Pid;
                reported = Term.Tuple(named, NodeAtom);
            }
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            ReferenceTerm reference = reservedReference ?? ReserveMonitorReference(observer, options);
            if (recipient?.Equals(observer.Self) == true)
            {
                aliases.Remove(reference);

                return reference;
            }
            var entry = CreateMonitorEntry641(
                observer,
                recipient,
                reported,
                options
            );
            AddMonitor641(reference, entry);
            if (recipient is null || !processes.ContainsKey(recipient))
                MonitorDown(
                    observer,
                    reference,
                    entry,
                    Term.A(recipient is not null && recipient.Node != Node ? ProcessMonitorFailureReasons.Disconnected : ErlangErrorReasons.NoProcess)
                );

            return reference;
        }
    }

    private ReferenceTerm MonitorSourceCore641(
        ProcessContext observer,
        string kind,
        Term target,
        ProcessMonitorOptions options
    )
    {
        if (kind == ProcessMonitorKinds.Process)
            return Monitor(observer, target, options);
        if (kind == ProcessMonitorKinds.Port)
            return MonitorPort(observer, target, options);
        if (kind != ProcessMonitorKinds.TimeOffset || target is not Atom { Name: ClockMonitorNames.Service })
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            RequireAlive(observer.Self);
            ReferenceTerm reference = options.AliasMode is null ? MakeReference() : CreateAlias(observer, options.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false);
            AddMonitor641(
                reference,
                CreateMonitorEntry641(
                    observer,
                    null,
                    target,
                    options,
                    ProcessMonitorSource.TimeOffset
                )
            );

            return reference;
        }
    }

    private void MonitorDown(
        ProcessContext observer,
        ReferenceTerm reference,
        ProcessMonitorEntry entry,
        Term reason,
        RuntimeProcessPayload641? reason641 = null
    )
    {
        SendMonitorPayload641(
            observer,
            Term.Tuple(
                entry.Tag,
                reference,
                Term.A(entry.Source == ProcessMonitorSource.Port ? ProcessMonitorKinds.Port : ProcessMonitorKinds.Process),
                entry.ReportedTarget,
                reason
            ),
            () =>
            {
                if (!monitors.TryGetValue(reference, out var current) || !ReferenceEquals(current, entry)
                    || !current.Observer.Equals(observer.Self))
                    return null;
                RemoveMonitor641(reference);
                if (!current.KeepAlias)
                    aliases.Remove(reference);

                return current.Priority;
            },
            entry.Custody641,
            reason641,
            from: entry.Target is { } target ? target.Node == Node ? target : Term.A(target.Node) : Term.A(ProcessTraceNames.System)
        );
    }

    public bool Demonitor(
        ProcessContext observer,
        ReferenceTerm reference,
        bool flush = false,
        bool info = false
    )
    {
        lock (gate)
        {
            if (reference.Node != Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            monitors.TryGetValue(reference, out var monitor);
            bool removed = !spawnRequests.ContainsKey(reference) &&
                distributionPending?.IsPendingConnectedSpawn(observer, reference) != true && monitor is not null && monitor.Observer.Equals(observer.Self);
            if (removed)
            {
                RemoveMonitor641(reference);
                if (!monitor!.KeepAlias)
                    aliases.Remove(reference);
                DetachConnectedSpawnMonitor(observer, reference);
            }
            if (flush && removed && monitor!.Source == ProcessMonitorSource.TimeOffset)
                observer.Mailbox.RemoveControl593(term => term is TupleTerm tuple && tuple.Items.Count == ProcessMonitorTupleLayout.Fields && tuple.Items[1].Equals(reference));
            else if (flush && !removed)
                observer.Mailbox.RemoveFirstControl593(term => term is TupleTerm tuple && tuple.Items.Count == ProcessMonitorTupleLayout.Fields && tuple.Items[1].Equals(reference));

            return !info || removed;
        }
    }

    public void Exit(ProcessContext sender, Pid target, Term reason)
    {
        var origin641 = RuntimeProcessPayload641.Prepare641(this, sender, reason);
        try
        {
            if (TryPublishBackedLocalExitAsync(sender, target, origin641.Value641)
                .GetAwaiter().GetResult())
                return;
            ExitPrepared641(
                sender,
                target,
                origin641.Value641,
                origin641
            );
        }
        finally
        {
            origin641.Close641();
        }
    }

    private void ExitPrepared641(
        ProcessContext sender,
        Pid target,
        Term reason,
        RuntimeProcessPayload641 origin641
    ) => ExitWithSequencePreparation(
        sender,
        target,
        reason,
        origin641
    );

    internal void SendExitSignal(
        ProcessContext sender,
        Term destination,
        Term reason,
        bool legacy,
        bool priority
    )
    {
        var captured641 = RuntimeProcessPayload641.Prepare641(this, sender, Term.Tuple(destination, reason));
        var pair641 = (TupleTerm)captured641.Value641;
        var origin641 = captured641.Borrow641(null);
        try
        {
            SendPreparedExit641(
                sender,
                pair641.Items[0],
                pair641.Items[1],
                legacy,
                priority,
                origin641
            );
        }
        finally
        {
            origin641.Close641();
            captured641.Close641();
        }
    }

    private void SendPreparedExit641(
        ProcessContext sender,
        Term destination,
        Term reason,
        bool legacy,
        bool priority,
        RuntimeProcessPayload641 complete641
    ) => SendPreparedExitWithSequence(
        sender,
        destination,
        reason,
        legacy,
        priority,
        complete641
    );

    private void QueueExit(
        ProcessContext target,
        Pid sender,
        Term reason,
        bool explicitSignal,
        bool normalKills,
        bool priority,
        ReferenceTerm? alias,
        bool linked,
        SequenceTraceToken? token = null,
        RuntimeProcessPayload641? reason641 = null
    ) => QueueExitWithSequenceControl(
        target,
        sender,
        reason,
        explicitSignal,
        normalKills,
        priority,
        alias,
        linked,
        token,
        reason641
    );

    private void Signal(
        ProcessContext target,
        Pid sender,
        Term reason,
        bool explicitSignal,
        RuntimeProcessPayload641? reason641 = null
    ) => Signal(
        target,
        sender,
        reason,
        explicitSignal,
        reason641,
        null
    );

    private void Terminate(
        ProcessContext ctx,
        Term reason,
        RuntimeProcessPayload641? reason641 = null,
        Exception? sourceTeardown641 = null
    ) => Terminate(
        ctx,
        reason,
        reason641,
        sourceTeardown641,
        null
    );

    // Called under runtime.gate after logical storage retirement. Host Done still joins physical work.
    private void FinishTermination(
        ProcessContext ctx,
        Term reason,
        List<Exception> failures,
        ProcessExitContinuation? exit,
        RuntimeProcessPayload641? reason641 = null
    ) => FinishTermination(
        ctx,
        reason,
        failures,
        exit,
        reason641,
        exit?.Sequence641
    );

    public ValueTask InitializeModulesAsync(Action<ModuleRegistry> configure)
    {
        if (Volatile.Read(ref haltResult) is not null)
            throw new ObjectDisposedException(nameof(ProcessRuntime));

        return initializer.Configure(configure);
    }

    internal void InitializationEnded(ProcessContext context)
        => InitializationEndedWithSequence(context);

    public async ValueTask DisposeAsync()
    {
        Modules.RequireNifShutdownCaller();
        RequireOwnedTermShutdownCaller506();
        var failures = new List<Exception>();
        PreparedSequenceDisposeStart shutdown = await StartSequenceDisposeAsync(failures).ConfigureAwait(false);
        Task sourceShutdown = shutdown.Source;
        Task nativeShutdown = shutdown.Native;
        // Actual physical drains run outside R; logical stop and cancellation
        // have already committed from the validated pre-close batch.
        ValueTask coreShutdown669 = shutdown.Core;
        Task? cancellationDrain669 = null;
        try
        {
            ProcessContext[] remaining;
            lock (gate)
                remaining = executingProcesses.ToArray();
            // Start ALL actual cancellations before joining any callback.
            Task[] cancellations = remaining.Select(async process =>
            {
                await process.Stop.CancelAsync().ConfigureAwait(false);
            }).ToArray();
            cancellationDrain669 = Task.WhenAll(cancellations);
            await cancellationDrain669.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(cancellationDrain669?.Exception ?? failure);
        }
        try
        {
            await coreShutdown669.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await DrainEtsRetirements().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await Task.WhenAll(
                DrainPersistentTerms(),
                DrainRegex411(),
                CloseBinaryPrograms598(),
                CloseSavedMatchPrograms634(),
                DrainSystemTasks428(),
                DrainDirtyNative419(),
                DrainDebugDispatch432(),
                DrainCounters421(),
                DrainMailboxRoots593(),
                DrainProcessPayloads641(),
                DrainAtomics586(),
                DrainHamtIndex429(),
                DrainDriverFrontend422(),
                DrainCodePurger432(),
                DrainAllocatorGather434(),
                DrainOwnedTermContexts506(),
                DrainNativeResources524(),
                KnownNodes.DrainPhysicalNodes(),
                DrainOrdinaryBinaries()
            ).ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        bool nativeImagesSettled528 = false;
        try
        {
            await Task.WhenAll(sourceShutdown, nativeShutdown, DrainAutomaticNifShutdown()).ConfigureAwait(false);
            nativeImagesSettled528 = true;
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            CloseNativeLinuxNamespace();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            FinalizeNativeAllocationArena528(nativeImagesSettled528);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        try
        {
            await CloseEmptyTuple803().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }

    internal bool AddPortLink(Pid pid, PortTerm port)
    {
        if (!processes.TryGetValue(pid, out var process))
            return false;
        process.PortLinks.Add(port);

        return true;
    }

    internal string? PortRegisteredName(PortTerm port) => names.FirstOrDefault(pair => pair.Value.Equals(port)).Key;

    internal void PortBadSignal(RuntimePort port)
        => QueuePortBadSignal454(port.Identity, port.Connected);

    internal void QueuePortBadSignal454(PortTerm identity, Pid connected)
        => QueuePortBadSignalWithSequence(identity, connected);

    internal void RegisterPort(string name, PortTerm port)
    {
        lock (gate)
        {
            if (!Ports.Alive(port) || name == ProcessRegistryAtoms.Undefined || names.ContainsKey(name) || names.ContainsValue(port))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            names.Add(name, port);
        }
    }

    internal void PortRemoved(
        PortTerm port,
        IReadOnlyList<Pid> links,
        Term reason,
        RuntimeProcessPayload641? reason641 = null
    )
    {
        SystemProfiling.Terminated(port);
        foreach (string name in names.Where(pair => pair.Value.Equals(port)).Select(pair => pair.Key).ToArray())
            names.Remove(name);
        foreach (Pid pid in links)
        {
            if (!processes.TryGetValue(pid, out var process))
                continue;
            var publication641 = RuntimeProcessPayload641.CombinePublication641(
                this,
                null,
                Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), port, reason),
                reason641,
                null
            );
            var terminal = CreatePendingSequenceTerminationControl(
                process,
                reason,
                () => !process.Terminated && process.PortLinks.Contains(port) && !process.TrapExits
                    && reason is not Atom { Name: ProcessExitReasons.Normal }
            );
            Func<bool?> admission = () =>
                {
                    if (process.Terminated || !process.PortLinks.Remove(port))
                        return null;
                    bool priority = process.PriorityPortLinks.Remove(port);
                    if (process.TrapExits)
                        return priority;
                    if (!reason.Equals(Term.A(ProcessExitReasons.Normal)))
                        Terminate(
                            process,
                            reason,
                            reason641,
                            null,
                            terminal.Prepared
                        );

                    return null;
                };
            RegisterPendingSequenceTermination(admission, terminal);
            try
            {
                process.DeferredSignalDrain ??= new();
                process.Mailbox.SendPendingSnapshot593(
                    publication641.Value641,
                    admission,
                    publication593: publication641.Publication641,
                    prepareBacking: true
                );
            }
            finally
            {
                publication641.Close641();
            }
        }
        foreach (var pair in monitors.Where(pair => pair.Value.PortTarget?.Equals(port) == true).ToArray())
            if (processes.TryGetValue(pair.Value.Observer, out var observer))
                MonitorDown(
                    observer,
                    pair.Key,
                    pair.Value,
                    reason,
                    reason641
                );
    }

    private ReferenceTerm MonitorPort(ProcessContext observer, Term target, ProcessMonitorOptions options)
    {
        lock (gate)
        {
            RequireAlive(observer.Self);
            Term reported;
            PortTerm? recipient;
            if (target is PortTerm identity && identity.Node == Node)
            {
                recipient = identity;
                reported = identity;
            }
            else if (target is Atom name)
            {
                recipient = WhereIs(name.Name) as PortTerm;
                reported = Term.Tuple(name, NodeAtom);
            }
            else if (target is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom registered && tuple.Items[1] is Atom node && node.Name == Node)
            {
                recipient = WhereIs(registered.Name) as PortTerm;
                reported = Term.Tuple(registered, NodeAtom);
            }
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            ReferenceTerm reference = options.AliasMode is null ? MakeReference() : CreateAlias(observer, options.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false);
            var entry = CreateMonitorEntry641(
                observer,
                null,
                reported,
                options,
                ProcessMonitorSource.Port,
                recipient
            );
            AddMonitor641(reference, entry);
            if (recipient is null || !Ports.Alive(recipient))
                MonitorDown(
                    observer,
                    reference,
                    entry,
                    Term.A(ErlangErrorReasons.NoProcess)
                );

            return reference;
        }
    }

    internal Term PortMonitoredBy(PortTerm port) => Cons.From(monitors.Values.Where(entry => entry.PortTarget?.Equals(port) == true).Select(entry => entry.Observer));
}
