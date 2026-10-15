namespace Erlang;

public sealed partial class ProcessRuntime : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<Pid, ProcessContext> processes = new();
    private readonly HashSet<ProcessContext> executingProcesses = new();
    private readonly SemaphoreSlim codePurger = new(1, 1);
    private readonly ProcessSlotTable processSlots;
    public int ProcessLimit => processSlots.Capacity;
    public int ReferenceDomainCapacity { get; } = Math.Min(Environment.ProcessorCount, (int)ReferenceNumberLayout.DomainMask);
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
    public int BacktraceDepth { get; internal set; } = ErlangStackLimits.DefaultBacktraceDepth;
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
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumActorConcurrency);
        processSlots = new(maximumProcesses);
        actorDispatcher = new(maximumActorConcurrency);
        AtomNameRegistry.Observe(node);
        ConfiguredNode = node;
        LocalIdentity = new(ProcessNodeDefaults.Local);
        ReferenceResources = new(Modules, LocalIdentity);
        KnownNodes = new(LocalIdentity);
        Modules.AttachRuntime(this);
        Output = output ?? Console.Out;
        ownsStandardStreams = standardStreams is null;
        StandardStreams = standardStreams ?? RuntimeStandardStreams.Open();
        Timers = new(this, gate);
        Suspensions = new(gate, processes, Timers);
        SystemMonitoring = new(this, gate, processes);
        Ports = new(this, gate);
        SequenceTracing = new(this, gate, processes);
        SystemProfiling = new(this, gate, processes);
        ProcessTracing = new(this, gate, processes);
        var initial = new ProcessContext(this, new Pid(LocalIdentity, InitializationNames.ProcessId));
        InitializationPid = initial.Self;
        processes.Add(initial.Self, initial);
        processSlots.Add(initial.Self);
        initializer = new(initial);
        initial.ExecutionStarted = true;
        initializer.Start();
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
        bool successSpawnReply = true
    )
    {
        ProcessContext ctx;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (link && parent is not null)
                RequireAlive(parent.Self);
            processSlots.RequireCapacity();
            ctx = new(
                this,
                new Pid(LocalIdentity, (ulong)++nextPid),
                description?.Mfa
            );
            ctx.GroupLeader = parent?.GroupLeader ?? processes[InitializationPid].GroupLeader;
            processes.Add(ctx.Self, ctx);
            executingProcesses.Add(ctx);
            processSlots.Add(ctx.Self);
            bool enqueued = false;
            try
            {
                _ = actorDispatcher.Dispatch(async () =>
                {
                    lock (gate)
                        ctx.ExecutionStarted = true;
                    Term reason = Term.A(ProcessExitReasons.Normal);
                    try
                    {
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
                                current = child =>
                                {
                                    return child.Runtime.Modules.Call(
                                        child,
                                        transfer.Module,
                                        transfer.Function,
                                        transfer.Arguments.ToArray()
                                    );
                                };
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
                    catch (OperationCanceledException) when (ctx.Cancellation.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (ErlangException ex)
                    {
                        reason = ex.ExceptionClass == ErlangExceptionClasses.Exit ? ex.Reason : Term.Tuple(
                            ex.ExceptionClass == ErlangExceptionClasses.Throw ? Term.Tuple(Term.A(ErlangErrorReasons.NoCatch), ex.Reason) : ex.Reason,
                            ex.StackTraceTerm
                        );
                    }
                    catch (Exception ex)
                    {
                        reason = Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(ex.GetType().Name));
                    }
                    finally
                    {
                        try
                        {
                            Terminate(ctx, reason);
                        }
                        finally
                        {
                            lock (gate)
                                executingProcesses.Remove(ctx);
                            ctx.Unwound.TrySetResult();
                        }
                    }
                });
                enqueued = true;
                if (link && parent is not null)
                {
                    EstablishSpawnLink(ctx, parent);
                }
                if (parent is not null)
                    SequenceTracing.Spawned(
                        parent,
                        ctx,
                        description,
                        spawnReference,
                        asynchronousSpawn,
                        successSpawnReply
                    );
            }
            catch
            {
                RollbackActorStart(ctx, enqueued);
                throw;
            }
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
        lock (gate)
        {
            RequireAlive(parent.Self);
            ReferenceTerm? reference = options.Monitor ? ReserveMonitorReference(parent, options.MonitorOptions) : null;
            var process = SpawnTraced(
                body,
                parent,
                options.Link,
                description,
                reference
            );
            if (options.PriorityLink)
                parent.PriorityLinks.Add(process.Pid);
            if (options.Monitor)
                return Term.Tuple(process.Pid, Monitor(
                    parent,
                    process.Pid,
                    options.MonitorOptions,
                    reference
                ));

            return process.Pid;
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
        lock (gate)
        {
            RequireAlive(observer.Self);
            ReferenceTerm reference = ReserveMonitorReference(observer, ProcessMonitorOptions.Default);
            var process = SpawnTraced(
                body,
                observer,
                link,
                description,
                reference
            );

            return (process, Monitor(
                observer,
                process.Pid,
                ProcessMonitorOptions.Default,
                reference
            ));
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
        lock (gate)
        {
            RequireAlive(owner.Self);
            ReferenceTerm ordinaryReference = MakeReference();
            ReferenceTerm reference = ordinaryReference;
            ProcessHandle? child = null;
            if (failure is null)
            {
                try
                {
                    processSlots.RequireCapacity();
                    if (options.Spawn.Monitor && options.Spawn.MonitorOptions.AliasMode is not null)
                    {
                        ulong ownerPid = (owner.Self.Id << ProcessAliasLayout.PidTagBits) | ProcessAliasLayout.PidTag;
                        reference = new ReferenceTerm(
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
                        options.SuccessReply
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
                if (options.Spawn.MonitorOptions.AliasMode is not null)
                    aliases.Add(reference, new(owner.Self, options.Spawn.MonitorOptions.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false));
                Monitor(
                    owner,
                    child.Pid,
                    options.Spawn.MonitorOptions,
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

    internal bool AbandonSpawnRequest(ProcessContext owner, ReferenceTerm reference)
    {
        lock (gate)
        {
            RequireAlive(owner.Self);
            if (!spawnRequests.TryGetValue(reference, out var request) || !request.Owner.Equals(owner.Self))
                return false;
            spawnRequests.Remove(reference);
            monitors.Remove(reference);
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
            var reference = new ReferenceTerm(
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
                        monitors.Remove(reference);
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

    internal void AdmitSignals(ProcessContext owner)
    {
        using var outputTransfer = distributionPending?.BeginOutputTransfer();
        if (owner.DeferredSignalDrain is { } drain && (drain.IsCurrent || owner.Mailbox.HasDeferredPending))
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
            return RequireAlive(owner.Self).GroupLeader;
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
        lock (gate)
        {
            if (!processes.TryGetValue(pid, out var process) || !ReferenceEquals(process, observed) || process.Terminated)
                return ProcessInfoAtoms.Undefined;
            var values = new List<Term>(keys.Count);
            IReadOnlyList<Term>? messages = keys.Any(key => key.Name is ProcessInfoNames.Messages or ProcessInfoNames.MessageQueueLength) ? process.Mailbox.Snapshot() : null;
            foreach (ProcessInfoItem key in keys)
            {
                Term value = key.Name switch
                {
                    ProcessInfoLogicalNames.Suspending => Suspensions.SnapshotOwned(process.Self),
                    ProcessInfoLogicalNames.InitialCall => process.InitialCall,
                    ProcessInfoLogicalNames.ErrorHandler => process.ErrorHandler,
                    ProcessInfoLogicalNames.Priority => process.SchedulingPriority,
                    ProcessInfoLogicalNames.Reductions => Term.I(process.CumulativeReductions),
                    ProcessInfoNames.Links => Cons.From(process.Links.Cast<Term>().Concat(process.PortLinks)),
                    ProcessInfoNames.PriorityMessages => Term.A(PriorityPermissionCount(process) != 0 ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                    ProcessInfoNames.TrapExit => Term.A(process.TrapExits ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                    ProcessInfoNames.GroupLeader => process.GroupLeader,
                    ProcessInfoNames.LastCalls => process.SavedCalls.Snapshot(),
                    ProcessInfoNames.Dictionary when key.DictionaryKey is { } dictionaryKey => process.Dictionary.GetValueOrDefault(dictionaryKey, ProcessInfoAtoms.Undefined),
                    ProcessInfoNames.Dictionary => Cons.From(process.Dictionary.Select(pair => Term.Tuple(pair.Key, pair.Value))),
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
        requester.Cancellation.ThrowIfCancellationRequested();
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
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(id) ? tuple : null);

        return reply!.Items[1];
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
        var image = await Modules.ModifyCodeAsync(worker, () => Modules.PreparePurge(module));
        if (image is null)
            return false;
        bool completed = false;
        try
        {
            await PurgeCodeImageProcesses(worker, image);
            await Modules.ModifyCodeAsync(() =>
                {
                    Modules.FinishPurge(module, image, true);

                    return true;
                });
            completed = true;

            return true;
        }
        finally
        {
            if (!completed)
                await Modules.ModifyCodeAsync(() =>
                {
                    Modules.FinishPurge(module, image, false);

                    return true;
                });
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
        ulong value = unchecked((ulong)Interlocked.Increment(ref nextRef));

        return new ReferenceTerm(
            LocalIdentity,
            [
            (uint)(value & ReferenceIdentityLayout.LocalFirstWordMask),
            (uint)value & ReferenceNumberLayout.CountMiddleMask | ReferenceNumberLayout.HostDomain,
            (uint)(value >> ReferenceNumberLayout.CountHighShift)
        ]
        );
    }

    public ReferenceTerm Monitor(ProcessContext observer, Pid target) => Monitor(observer, target, ProcessMonitorOptions.Default);

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
            var entry = new ProcessMonitorEntry(
                observer.Self,
                recipient,
                reported,
                options.Tag,
                options.Priority,
                options.AliasMode == ProcessAliasNames.Explicit
            );
            monitors.Add(reference, entry);
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

    internal ReferenceTerm MonitorSource(
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
            monitors.Add(
                reference,
                new ProcessMonitorEntry(
                    observer.Self,
                    null,
                    target,
                    options.Tag,
                    options.Priority,
                    options.AliasMode == ProcessAliasNames.Explicit,
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
        Term reason
    )
    {
        observer.Mailbox.SendPending(
            Term.Tuple(
                entry.Tag,
                reference,
                Term.A(entry.Source == ProcessMonitorSource.Port ? ProcessMonitorKinds.Port : ProcessMonitorKinds.Process),
                entry.ReportedTarget,
                reason
            ),
            () =>
        {
            if (!monitors.TryGetValue(reference, out var current) || !current.Observer.Equals(observer.Self))
                return null;
            monitors.Remove(reference);
            if (!current.KeepAlias)
                aliases.Remove(reference);

            return current.Priority;
        },
            preserve: true,
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
            bool removed = !spawnRequests.ContainsKey(reference) && monitor is not null && monitor.Observer.Equals(observer.Self);
            if (removed)
            {
                monitors.Remove(reference);
                if (!monitor!.KeepAlias)
                    aliases.Remove(reference);
            }
            if (flush && removed && monitor!.Source == ProcessMonitorSource.TimeOffset)
                observer.Mailbox.Remove(term => term is TupleTerm tuple && tuple.Items.Count == ProcessMonitorTupleLayout.Fields && tuple.Items[1].Equals(reference));
            else if (flush && !removed)
                observer.Mailbox.RemoveFirst(term => term is TupleTerm tuple && tuple.Items.Count == ProcessMonitorTupleLayout.Fields && tuple.Items[1].Equals(reference));

            return !info || removed;
        }
    }

    public void Exit(ProcessContext sender, Pid target, Term reason)
    {
        lock (gate)
        {
            if (processes.TryGetValue(target, out var p))
                Signal(
                    p,
                    sender.Self,
                    reason,
                    true
                );
        }
    }

    internal void SendExitSignal(
        ProcessContext sender,
        Term destination,
        Term reason,
        bool legacy,
        bool priority
    )
    {
        if (destination is PortTerm port)
        {
            Ports.SendExit(sender, port, reason);

            return;
        }
        if (destination is not Pid && destination is not ReferenceTerm)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        lock (gate)
        {
            if (destination is Pid pid)
            {
                if (!processes.TryGetValue(pid, out var target))
                    return;
                bool normalKills = legacy && pid.Equals(sender.Self);
                if (normalKills && (reason.Equals(Term.A(ProcessExitSignals.Kill)) || !target.TrapExits))
                {
                    Terminate(target, reason.Equals(Term.A(ProcessExitSignals.Kill)) ? Term.A(ProcessExitReasons.Killed) : reason);
                    sender.Cancellation.ThrowIfCancellationRequested();

                    return;
                }
                QueueExit(
                    target,
                    sender.Self,
                    reason,
                    true,
                    normalKills,
                    false,
                    null,
                    false
                );
            }
            else if (destination is ReferenceTerm reference && aliases.TryGetValue(reference, out var alias) && processes.TryGetValue(alias.Owner, out var target))
                QueueExit(
                    target,
                    sender.Self,
                    reason,
                    true,
                    false,
                    priority,
                    reference,
                    false
                );
        }
    }

    private void QueueExit(
        ProcessContext target,
        Pid sender,
        Term reason,
        bool explicitSignal,
        bool normalKills,
        bool priority,
        ReferenceTerm? alias,
        bool linked,
        SequenceTraceToken? token = null
    )
    {
        target.Mailbox.SendPending(
            Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender, reason),
            () =>
        {
            if (target.Terminated)
                return null;
            bool prioritized = false;
            if (alias is not null)
            {
                if (!aliases.TryGetValue(alias, out var permission) || !permission.Owner.Equals(target.Self))
                    return null;
                prioritized = priority && permission.Priority;
            }
            if (linked)
            {
                target.LinkIdentities.Remove(sender);
                if (!target.Links.Remove(sender))
                    return null;
                prioritized = target.PriorityLinks.Remove(sender);
            }
            bool kill = explicitSignal && reason.Equals(Term.A(ProcessExitSignals.Kill));
            if (target.TrapExits && !kill)
                return prioritized;
            if (!kill && reason.Equals(Term.A(ProcessExitReasons.Normal)) && !normalKills)
                return null;
            Terminate(target, kill ? Term.A(ProcessExitReasons.Killed) : reason);

            return null;
        },
            token: token,
            from: sender
        );
    }

    private void Signal(
        ProcessContext target,
        Pid sender,
        Term reason,
        bool explicitSignal
    )
    {
        bool kill = explicitSignal && reason.Equals(Term.A(ProcessExitSignals.Kill));
        if (target.TrapExits && !kill)
        {
            target.Mailbox.SendTraced(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender, reason), null, from: sender);

            return;
        }
        if (!kill && reason.Equals(Term.A(ProcessExitReasons.Normal)) && !sender.Equals(target.Self))
            return;
        Terminate(target, kill ? Term.A(ProcessExitReasons.Killed) : reason);
    }

    private void Terminate(ProcessContext ctx, Term reason)
    {
        lock (gate)
        {
            if (!processes.Remove(ctx.Self))
                return;
            var failures = new List<Exception>();
            ProcessExitContinuation? exit = null;
            // Public FREE and runtime-owned cleanup have separate lifetimes.
            processSlots.Remove(ctx.Self);
            ctx.Terminated = true;
            ctx.ExitReason = reason;
            if (ctx.Self.Equals(InitializationPid))
                CleanupStep(StartNifShutdown);
            CleanupStep(() =>
            {
                var remoteOutput = CaptureDistributionExitOutput(ctx, reason);
                if (remoteOutput.Count != 0)
                {
                    exit = new(ctx, reason, remoteOutput);
                    StartExitContinuation(exit);
                }
            });
            CleanupStep(() => LocalNodeProcessTerminated(ctx, reason));
            CleanupStep(() => distributionPending?.ProcessTerminated(ctx, reason));
            CleanupStep(() => ProcessTracing.Terminated(ctx.Self));
            CleanupStep(() => SystemMonitoring.Terminated(ctx.Self));
            CleanupStep(() => SystemProfiling.Terminated(ctx.Self));
            foreach (var request in ctx.PendingCodeChecks)
                CleanupStep(() => ReplyCodeCheck(request, false, ctx.Self));
            ctx.PendingCodeChecks.Clear();
            foreach (var request in ctx.PendingInfoRequests)
                CleanupStep(() => ReplyProcessInfo(request, ProcessInfoAtoms.Undefined, ctx.Self));
            ctx.PendingInfoRequests.Clear();
            foreach (var request in ctx.PendingDisplayRequests)
                CleanupStep(() => ReplyProcessDisplay(request, Term.A(ErlangErrorReasons.BadArgument)));
            ctx.PendingDisplayRequests.Clear();
            foreach (var request in ctx.PendingGarbageCollections)
                CleanupStep(() => ReplyGarbageCollection(request, false, ctx.Self));
            ctx.PendingGarbageCollections.Clear();
            foreach (var request in ctx.PendingSavedCallRequests)
                CleanupStep(() => ReplySavedCallRequest(request, Term.A(ErlangErrorReasons.BadArgument), ctx.Self));
            ctx.PendingSavedCallRequests.Clear();
            CleanupStep(() => Timers.RecipientTerminated(ctx.Self));
            CleanupStep(() => Suspensions.Terminated(ctx));
            CleanupStep(ctx.Mailbox.DiscardPending);
            foreach (var request in spawnRequests.Where(pair => pair.Value.Owner.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
                spawnRequests.Remove(request);
            foreach (var alias in aliases.Where(pair => pair.Value.Owner.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
                aliases.Remove(alias);
            CleanupStep(() => Ports.OwnerTerminated(ctx, reason));
            foreach (var name in names.Where(pair => pair.Value.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
                names.Remove(name);
            var linkedExits = ctx.Links.Select(pid => (Pid: pid, Reason: reason))
                .Concat(ctx.PendingLinkOrigins.Values.Select(pid => (Pid: pid, Reason: (Term)Term.A(ErlangErrorReasons.NoProcess))))
                .ToArray();
            ctx.PendingLinkOrigins.Clear();
            ctx.Links.Clear();
            ctx.LinkIdentities.Clear();
            ctx.PriorityLinks.Clear();
            foreach (var monitor in monitors.ToArray())
            {
                if (monitor.Value.Target?.Equals(ctx.Self) == true)
                {
                    if (processes.TryGetValue(monitor.Value.Observer, out var observer))
                        CleanupStep(() => MonitorDown(
                            observer,
                            monitor.Key,
                            monitor.Value,
                            reason
                        ));
                }
                else if (monitor.Value.Observer.Equals(ctx.Self))
                    monitors.Remove(monitor.Key);
            }
            CleanupStep(ctx.Stop.Cancel);
            foreach (var linkedExit in linkedExits)
            {
                CleanupStep(() =>
                {
                    SequenceTraceToken? exitToken = SequenceTracing.LinkedExit(ctx, linkedExit.Pid, linkedExit.Reason);
                    if (processes.TryGetValue(linkedExit.Pid, out var linked))
                    {
                        QueueExit(
                            linked,
                            ctx.Self,
                            linkedExit.Reason,
                            false,
                            false,
                            false,
                            null,
                            true,
                            exitToken
                        );
                    }
                });
            }
            if (failures.Count == 0)
            {
                if (exit is not null)
                    exit.LocalCleanup.TrySetResult();
                else
                    ctx.Done.TrySetResult(reason);
            }
            else
            {
                var failure = new AggregateException(failures);
                if (exit is not null)
                    exit.LocalCleanup.TrySetException(failure);
                else
                {
                    exitContinuationFailures.Add(failure);
                    ctx.Done.TrySetException(failure);
                }
                throw failure;
            }

            void CleanupStep(Action cleanup)
            {
                try
                {
                    cleanup();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }
    }

    public ValueTask InitializeModulesAsync(Action<ModuleRegistry> configure)
    {
        if (Volatile.Read(ref haltResult) is not null)
            throw new ObjectDisposedException(nameof(ProcessRuntime));

        return initializer.Configure(configure);
    }

    internal void InitializationEnded(ProcessContext context)
    {
        try
        {
            lock (gate)
            {
                Terminate(context, context.ExitReason ?? Term.A(ProcessExitReasons.Shutdown));
                if (disposed)
                    return;
                StopLocalNode();
                localNodeEvents?.Close();
                disposed = true;
                _ = actorDispatcher.CompleteWhenIdle();
                Timers.Dispose();
                distributionPending?.Close();
                ReferenceResources.Close();
                Ports.Shutdown();
                foreach (var process in processes.Values.ToArray())
                    Terminate(process, Term.A(ProcessExitReasons.Shutdown));
            }
        }
        finally
        {
            StartNifShutdown();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Modules.RequireNifShutdownCaller();
        var failures = new List<Exception>();
        Task nativeShutdown;
        try
        {
            nativeShutdown = Modules.StartOwnedNifShutdown();
        }
        catch (Exception failure)
        {
            failures.Add(failure);
            nativeShutdown = Task.CompletedTask;
        }
        try
        {
            await DisposeRuntimeCoreAsync().ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(failure);
        }
        // A failing cancellation callback must not leave another native owner running.
        ProcessContext[] remaining;
        lock (gate)
            remaining = executingProcesses.ToArray();
        Task[] cancellations = remaining.Select(async process =>
        {
            await process.Stop.CancelAsync().ConfigureAwait(false);
        }).ToArray();
        Task cancellationDrain = Task.WhenAll(cancellations);
        try
        {
            await cancellationDrain.ConfigureAwait(false);
        }
        catch (Exception failure)
        {
            failures.Add(cancellationDrain.Exception ?? failure);
        }
        try
        {
            await Task.WhenAll(nativeShutdown, DrainAutomaticNifShutdown()).ConfigureAwait(false);
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

    private async ValueTask DisposeRuntimeCoreAsync()
    {
        Task? haltWait;
        lock (gate)
        {
            haltWait = haltResult is null ? null : haltQuiescence.Task;
            if (haltWait is null)
            {
                StopLocalNode();
                localNodeEvents?.Close();
                disposed = true;
                _ = actorDispatcher.CompleteWhenIdle();
                Timers.Dispose();
                distributionPending?.Close();
                ReferenceResources.Close();
                Ports.Shutdown();
                foreach (var p in processes.Values.ToArray())
                    Terminate(p, Term.A(ProcessExitReasons.Shutdown));
            }
        }
        if (haltWait is not null)
        {
            await haltWait.ConfigureAwait(false);

            return;
        }
        await initializer.Completion.ConfigureAwait(false);
        await Ports.Drain().ConfigureAwait(false);
        await Task.WhenAll(
            distributionPending?.DrainOwnedOutputOperations() ?? Task.CompletedTask,
            DrainExitContinuations()
        ).ConfigureAwait(false);
        if (ownsStandardStreams)
            StandardStreams.Dispose();
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
    {
        if (!processes.TryGetValue(port.Connected, out var process))
            return;
        Term reason = Term.A(PortNames.BadSignal);
        process.Mailbox.SendPending(
            Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), port.Identity, reason),
            () =>
        {
            if (process.Terminated)
                return null;
            if (process.TrapExits)
                return false;
            Terminate(process, reason);

            return null;
        }
        );
    }

    internal void RegisterPort(string name, PortTerm port)
    {
        lock (gate)
        {
            if (!Ports.Alive(port) || name == ProcessRegistryAtoms.Undefined || names.ContainsKey(name) || names.ContainsValue(port))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            names.Add(name, port);
        }
    }

    internal void PortRemoved(PortTerm port, IReadOnlyList<Pid> links, Term reason)
    {
        SystemProfiling.Terminated(port);
        foreach (string name in names.Where(pair => pair.Value.Equals(port)).Select(pair => pair.Key).ToArray())
            names.Remove(name);
        foreach (Pid pid in links)
        {
            if (!processes.TryGetValue(pid, out var process))
                continue;
            process.Mailbox.SendPending(
                Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), port, reason),
                () =>
            {
                if (process.Terminated || !process.PortLinks.Remove(port))
                    return null;
                bool priority = process.PriorityPortLinks.Remove(port);
                if (process.TrapExits)
                    return priority;
                if (!reason.Equals(Term.A(ProcessExitReasons.Normal)))
                    Terminate(process, reason);

                return null;
            }
            );
        }
        foreach (var pair in monitors.Where(pair => pair.Value.PortTarget?.Equals(port) == true).ToArray())
            if (processes.TryGetValue(pair.Value.Observer, out var observer))
                MonitorDown(
                    observer,
                    pair.Key,
                    pair.Value,
                    reason
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
            var entry = new ProcessMonitorEntry(
                observer.Self,
                null,
                reported,
                options.Tag,
                options.Priority,
                options.AliasMode == ProcessAliasNames.Explicit,
                ProcessMonitorSource.Port,
                recipient
            );
            monitors.Add(reference, entry);
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
