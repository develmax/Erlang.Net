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
    internal RuntimePorts Ports { get; }
    private readonly Dictionary<ReferenceTerm, ProcessMonitorEntry> monitors = new();
    private readonly Dictionary<ReferenceTerm, ProcessAliasEntry> aliases = new();
    private long nextPid, nextRef;
    private bool disposed;
    private readonly RuntimeInitializer initializer;
    public Pid InitializationPid { get; }
    public string Node
    {
        get;
    }
    public Atom NodeAtom { get; }
    public TextWriter Output
    {
        get;
    }
    internal RuntimeTimers Timers { get; }
    internal RuntimeKnownNodes KnownNodes { get; }
    internal ProcessSuspensions Suspensions { get; }
    public RuntimeStandardStreams StandardStreams { get; }
    private readonly bool ownsStandardStreams;
    public int BacktraceDepth { get; internal set; } = ErlangStackLimits.DefaultBacktraceDepth;
    public ModuleRegistry Modules { get; } = new();
    public RuntimeFunctionEntries Functions { get; } = new();
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

    public ProcessRuntime(
        string node,
        TextWriter? output,
        int maximumProcesses,
        RuntimeStandardStreams? standardStreams
    )
    {
        processSlots = new(maximumProcesses);
        Node = node;
        NodeAtom = Term.A(node);
        KnownNodes = new(node);
        Output = output ?? Console.Out;
        ownsStandardStreams = standardStreams is null;
        StandardStreams = standardStreams ?? RuntimeStandardStreams.Open();
        Timers = new(this, gate);
        Suspensions = new(gate, processes, Timers);
        Ports = new(this, gate);
        var initial = new ProcessContext(this, new Pid(Node, InitializationNames.ProcessId));
        InitializationPid = initial.Self;
        processes.Add(initial.Self, initial);
        processSlots.Add(initial.Self);
        initializer = new(initial);
        initializer.Start();
    }

    public ProcessHandle Spawn(Func<ProcessContext, ValueTask<Term>> body, ProcessContext? parent = null, bool link = false)
    {
        ProcessContext ctx;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (link && parent is not null)
                RequireAlive(parent.Self);
            processSlots.RequireCapacity();
            ctx = new(this, new Pid(Node, (ulong)++nextPid));
            ctx.GroupLeader = parent?.GroupLeader ?? processes[InitializationPid].GroupLeader;
            processes.Add(ctx.Self, ctx);
            executingProcesses.Add(ctx);
            processSlots.Add(ctx.Self);
            if (link && parent is not null)
            {
                ctx.Links.Add(parent.Self);
                parent.Links.Add(ctx.Self);
            }
        }
        _ = Task.Run(async () =>
        {
            Term reason = Term.A(ProcessExitReasons.Normal);
            try
            {
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
                Terminate(ctx, reason);
                lock (gate)
                    executingProcesses.Remove(ctx);
                ctx.Unwound.TrySetResult();
            }
        });

        return new(ctx.Self, ctx.Done.Task);
    }

    internal Term SpawnWithOptions(ProcessContext parent, Func<ProcessContext, ValueTask<Term>> body, ProcessSpawnOptions options)
    {
        lock (gate)
        {
            RequireAlive(parent.Self);
            var process = Spawn(body, parent, options.Link);
            if (options.PriorityLink)
                parent.PriorityLinks.Add(process.Pid);
            if (options.Monitor)
                return Term.Tuple(process.Pid, Monitor(parent, process.Pid, options.MonitorOptions));

            return process.Pid;
        }
    }

    public (ProcessHandle Process, ReferenceTerm Reference) SpawnMonitor(ProcessContext observer, Func<ProcessContext, ValueTask<Term>> body, bool link = false)
    {
        lock (gate)
        {
            RequireAlive(observer.Self);
            var process = Spawn(body, observer, link);

            return (process, Monitor(observer, process.Pid));
        }
    }

    public Term Send(Pid target, Term message)
    {
        lock (gate)
        {
            if (processes.TryGetValue(target, out var p))
                p.Mailbox.Send(message);
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
                Node,
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
        lock (gate)
        {
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
                }
                );
            }
        }

        return message;
    }

    internal void AdmitSignals(ProcessContext owner)
    {
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

    internal Term ProcessInfo(Pid pid, IReadOnlyList<Atom> keys, bool singleton)
    {
        lock (gate)
        {
            if (!processes.TryGetValue(pid, out var process))
                return ProcessInfoAtoms.Undefined;
            if (keys.Any(key => key.Name == ProcessInfoNames.Links))
            {
                AdmitSignals(process);
                if (process.Terminated)
                    return ProcessInfoAtoms.Undefined;
            }
            var values = new List<Term>(keys.Count);
            foreach (Atom key in keys)
            {
                Term value = key.Name switch
                {
                    ProcessInfoNames.Links => Cons.From(process.Links.Cast<Term>().Concat(process.PortLinks)),
                    ProcessInfoNames.TrapExit => Term.A(process.TrapExits ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False),
                    ProcessInfoNames.GroupLeader => process.GroupLeader,
                    ProcessInfoNames.LastCalls => process.SavedCalls.Snapshot(),
                    ProcessInfoNames.RegisteredName => names.FirstOrDefault(pair => pair.Value.Equals(pid)).Key is string name ? Term.A(name) : Nil.Value,
                    _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                };
                if (singleton && key.Name == ProcessInfoNames.RegisteredName && value is Nil)
                    return Nil.Value;
                values.Add(Term.Tuple(key, value));
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
                            ReplySavedCallRequest(request, ApplySavedCallFlag(process, request.Flag, request.Value));
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

    private void ReplySavedCallRequest(ProcessSavedCallRequest request, Term result) => Send(request.Requester.Self, Term.Tuple(request.Id, result));

    internal async ValueTask<Term> CheckProcessCode(
        ProcessContext requester,
        Pid target,
        string module,
        bool async,
        Term requestId
    )
    {
        requester.Cancellation.ThrowIfCancellationRequested();
        if (!async && target.Equals(requester.Self))
            return Term.A(Modules.IsExecutingOldCode(requester, module) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
        if (!async)
            requestId = MakeReference();
        var request = new ProcessCodeRequest(requester, requestId, module);
        lock (gate)
        {
            if (!processes.TryGetValue(target, out var process))
                ReplyCodeCheck(request, false);
            else
            {
                process.PendingCodeChecks.Add(request);
                process.Mailbox.SendPending(
                    Nil.Value,
                    () =>
                {
                    lock (gate)
                    {
                        if (process.PendingCodeChecks.Remove(request))
                            ReplyCodeCheck(request, Modules.IsExecutingOldCode(process, module));
                    }

                    return null;
                }
                );
            }
        }
        if (async)
            return ProcessCodeAtoms.AsyncResult;
        var reply = await requester.ReceiveAsync(message => message is TupleTerm { Items.Count: 3 } tuple &&
            tuple.Items[0] is Atom { Name: ProcessCodeCheckNames.ReplyTag } && tuple.Items[1].Equals(requestId) ? tuple : null);

        return reply!.Items[2];
    }

    private void ReplyCodeCheck(ProcessCodeRequest request, bool result) => Send(
        request.Requester.Self,
        Term.Tuple(
        ProcessCodeAtoms.ReplyTag,
        request.Id,
        Term.A(result ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)
    )
    );

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
        var image = Modules.PreparePurge(module);
        if (image is null)
            return false;
        bool completed = false;
        try
        {
            ProcessContext[] candidates;
            lock (gate)
                candidates = processes.Values.Concat(executingProcesses).Distinct().ToArray();
            foreach (var candidate in candidates)
            {
                if (ReferenceEquals(candidate, worker))
                    continue;
                bool terminated;
                lock (gate)
                    terminated = candidate.Terminated;
                if (terminated)
                {
                    if (candidate.IsExecutingCode(image))
                        await candidate.Unwound.Task.WaitAsync(worker.Cancellation);
                    continue;
                }
                if (!(await CheckProcessCode(
                    worker,
                    candidate.Self,
                    module,
                    false,
                    Nil.Value
                )).Equals(Term.A(ErlangBooleanAtoms.True)))
                {
                    if (candidate.IsExecutingCode(image))
                        await candidate.Unwound.Task.WaitAsync(worker.Cancellation);
                    continue;
                }
                var monitor = Monitor(worker, candidate.Self);
                Exit(worker, candidate.Self, Term.A(ProcessExitSignals.Kill));
                await worker.ReceiveAsync(message => message is TupleTerm { Items.Count: 5 } tuple &&
                    tuple.Items[0] is Atom { Name: ProcessMessageTags.MonitorDown } && tuple.Items[1].Equals(monitor) ? tuple : null);
                await candidate.Unwound.Task.WaitAsync(worker.Cancellation);
            }
            Modules.FinishPurge(module, image, true);
            completed = true;

            return true;
        }
        finally
        {
            if (!completed)
                Modules.FinishPurge(module, image, false);
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
                source.Links.Add(target);
                process.Links.Add(source.Self);
                if (priority)
                    source.PriorityLinks.Add(target);
                else
                    source.PriorityLinks.Remove(target);

                return;
            }
            if (!source.TrapExits && target.Node == Node)
                throw new ErlangException(ErlangErrorReasons.NoProcess);
            source.Links.Add(target);
            if (priority)
                source.PriorityLinks.Add(target);
            else
                source.PriorityLinks.Remove(target);
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
            if (processes.TryGetValue(target, out var p))
            {
                p.Links.Remove(source.Self);
                p.PriorityLinks.Remove(source.Self);
            }
        }
    }

    public ReferenceTerm MakeReference()
    {
        ulong value = unchecked((ulong)Interlocked.Increment(ref nextRef));

        return new ReferenceTerm(
            Node,
            [
            (uint)(value & ReferenceIdentityLayout.LocalFirstWordMask),
            (uint)value & ReferenceNumberLayout.CountMiddleMask | ReferenceNumberLayout.HostDomain,
            (uint)(value >> ReferenceNumberLayout.CountHighShift)
        ]
        );
    }

    public ReferenceTerm Monitor(ProcessContext observer, Pid target) => Monitor(observer, target, ProcessMonitorOptions.Default);

    internal ReferenceTerm Monitor(ProcessContext observer, Term target, ProcessMonitorOptions options)
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
            ReferenceTerm reference = options.AliasMode is null ? MakeReference() : CreateAlias(observer, options.AliasMode == ProcessMonitorOptionNames.ReplyDemonitor, false);
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
        }
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
            bool removed = monitors.TryGetValue(reference, out var monitor) && monitor.Observer.Equals(observer.Self);
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
        bool linked
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
        }
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
            target.Mailbox.Send(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), sender, reason));

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
            var pending = new Queue<(ProcessContext Context, Term Reason)>();
            pending.Enqueue((ctx, reason));
            while (pending.TryDequeue(out var death))
            {
                ctx = death.Context;
                reason = death.Reason;
                if (!processes.Remove(ctx.Self))
                    continue;
                processSlots.Remove(ctx.Self);
                foreach (var request in ctx.PendingCodeChecks)
                    ReplyCodeCheck(request, false);
                ctx.PendingCodeChecks.Clear();
                foreach (var request in ctx.PendingGarbageCollections)
                    ReplyGarbageCollection(request, false);
                ctx.PendingGarbageCollections.Clear();
                foreach (var request in ctx.PendingSavedCallRequests)
                    ReplySavedCallRequest(request, Term.A(ErlangErrorReasons.BadArgument));
                ctx.PendingSavedCallRequests.Clear();
                Timers.RecipientTerminated(ctx.Self);
                Suspensions.Terminated(ctx);
                ctx.Mailbox.DiscardPending();
                foreach (var alias in aliases.Where(pair => pair.Value.Owner.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
                    aliases.Remove(alias);
                ctx.Terminated = true;
                ctx.ExitReason = reason;
                Ports.OwnerTerminated(ctx, reason);
                foreach (var name in names.Where(p => p.Value.Equals(ctx.Self)).Select(p => p.Key).ToArray())
                    names.Remove(name);
                var links = ctx.Links.ToArray();
                ctx.Links.Clear();
                ctx.PriorityLinks.Clear();
                foreach (var m in monitors.ToArray())
                {
                    if (m.Value.Target?.Equals(ctx.Self) == true)
                    {
                        if (processes.TryGetValue(m.Value.Observer, out var observer))
                            MonitorDown(
                                observer,
                                m.Key,
                                m.Value,
                                reason
                            );
                    }
                    else if (m.Value.Observer.Equals(ctx.Self))
                        monitors.Remove(m.Key);
                }
                ctx.Stop.Cancel();
                ctx.Done.TrySetResult(reason);
                foreach (var pid in links)
                    if (processes.TryGetValue(pid, out var linked))
                    {
                        QueueExit(
                            linked,
                            ctx.Self,
                            reason,
                            false,
                            false,
                            false,
                            null,
                            true
                        );
                    }
            }
        }
    }

    public ValueTask InitializeModulesAsync(Action<ModuleRegistry> configure) => initializer.Configure(configure);

    internal void InitializationEnded(ProcessContext context)
    {
        lock (gate)
        {
            Terminate(context, context.ExitReason ?? Term.A(ProcessExitReasons.Shutdown));
            if (disposed)
                return;
            disposed = true;
            Timers.Dispose();
            Ports.Shutdown();
            foreach (var process in processes.Values.ToArray())
                Terminate(process, Term.A(ProcessExitReasons.Shutdown));
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (gate)
        {
            disposed = true;
            Timers.Dispose();
            Ports.Shutdown();
            foreach (var p in processes.Values.ToArray())
                Terminate(p, Term.A(ProcessExitReasons.Shutdown));
        }

        await initializer.Completion.ConfigureAwait(false);
        await Ports.Drain().ConfigureAwait(false);
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
