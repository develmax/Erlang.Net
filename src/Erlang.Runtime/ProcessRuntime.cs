namespace Erlang;

public sealed class ProcessRuntime : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<Pid, ProcessContext> processes = new();
    private readonly Dictionary<string, Pid> names = new(StringComparer.Ordinal);
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
    public int BacktraceDepth { get; internal set; } = ErlangStackLimits.DefaultBacktraceDepth;
    public ModuleRegistry Modules { get; } = new();
    public RuntimeFunctionEntries Functions { get; } = new();
    internal uint LocalTermSeed { get; } = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(System.Security.Cryptography.RandomNumberGenerator.GetBytes(ErlangEtfParameters.LocalSeedBytes));

    public ProcessRuntime(string node = ProcessNodeDefaults.Local, TextWriter? output = null)
    {
        Node = node;
        NodeAtom = Term.A(node);
        Output = output ?? Console.Out;
        Timers = new(this, gate);
        var initial = new ProcessContext(this, new Pid(Node, InitializationNames.ProcessId));
        InitializationPid = initial.Self;
        processes.Add(initial.Self, initial);
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
            ctx = new(this, new Pid(Node, (ulong)++nextPid));
            ctx.GroupLeader = parent?.GroupLeader ?? processes[InitializationPid].GroupLeader;
            processes.Add(ctx.Self, ctx);
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
                await body(ctx);
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
            }
        });

        return new(ctx.Self, ctx.Done.Task);
    }

    public (ProcessHandle Process, ReferenceTerm Reference) SpawnMonitor(ProcessContext observer, Func<ProcessContext, ValueTask<Term>> body)
    {
        lock (gate)
        {
            RequireAlive(observer.Self);
            var process = Spawn(body, observer);

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
            (uint)(value >> ReferenceIdentityLayout.LocalFirstWordBits),
            (uint)(value >> (ReferenceIdentityLayout.LocalFirstWordBits + ReferenceIdentityLayout.WordBits))
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
                Term.A(ProcessMonitorKinds.Process),
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
            else if (flush)
                observer.Mailbox.RemoveFirst(term => term is TupleTerm tuple && tuple.Items.Count == 5 && tuple.Items[1].Equals(reference));

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
                Timers.RecipientTerminated(ctx.Self);
                ctx.Mailbox.DiscardPending();
                foreach (var alias in aliases.Where(pair => pair.Value.Owner.Equals(ctx.Self)).Select(pair => pair.Key).ToArray())
                    aliases.Remove(alias);
                ctx.Terminated = true;
                ctx.ExitReason = reason;
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
            foreach (var p in processes.Values.ToArray())
                Terminate(p, Term.A(ProcessExitReasons.Shutdown));
        }

        await initializer.Completion.ConfigureAwait(false);
    }
}
