

namespace Erlang;

public sealed class ProcessRuntime : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<Pid, ProcessContext> processes = new();
    private readonly Dictionary<string, Pid> names = new(StringComparer.Ordinal);
    private readonly Dictionary<ReferenceTerm, (Pid Observer, Pid Target)> monitors = new();
    private long nextPid, nextRef;
    private bool disposed;
    public string Node
    {
        get;
    }
    public TextWriter Output
    {
        get;
    }
    public ModuleRegistry Modules { get; } = new();
    public ProcessRuntime(string node = "nonode@nohost", TextWriter? output = null)
    {
        Node = node;
        Output = output ?? Console.Out;
        CoreModules.Register(Modules);
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
            processes.Add(ctx.Self, ctx);
            if (link && parent is not null)
            {
                ctx.Links.Add(parent.Self);
                parent.Links.Add(ctx.Self);
            }
        }
        _ = Task.Run(async () =>
        {
            Term reason = Term.A("normal");
            try
            {
                ctx.Cancellation.ThrowIfCancellationRequested();
                await body(ctx);
            }
            catch (OperationCanceledException) when (ctx.Cancellation.IsCancellationRequested) { return; }
            catch (ErlangException ex)
            {
                reason = ex.ExceptionClass == ErlangExceptionClasses.Exit ? ex.Reason : Term.Tuple(ex.Reason, Nil.Value);
            }
            catch (Exception ex) { reason = Term.Tuple(Term.A("clr_error"), Term.String(ex.GetType().Name)); }
            finally { Terminate(ctx, reason); }
        });
        return new(ctx.Self, ctx.Done.Task);
    }
    public (ProcessHandle Process, ReferenceTerm Reference) SpawnMonitor(ProcessContext observer, Func<ProcessContext, ValueTask<Term>> body)
    {
        lock (gate)
        {
            RequireAlive(observer.Self);
            var process = Spawn(body);
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
    public bool IsAlive(Pid pid)
    {
        lock (gate)
            return processes.ContainsKey(pid);
    }
    public void Register(string name, Pid pid)
    {
        lock (gate)
        {
            RequireAlive(pid);
            if (name == "undefined" || names.ContainsKey(name) || names.ContainsValue(pid))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            names.Add(name, pid);
        }
    }
    public Term WhereIs(string name)
    {
        lock (gate)
            return names.TryGetValue(name, out var p) ? p : Term.A("undefined");
    }
    public void Unregister(string name)
    {
        lock (gate)
        {
            if (!names.Remove(name))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }
    private ProcessContext RequireAlive(Pid pid) => processes.TryGetValue(pid, out var p) ? p : throw new ErlangException("noproc");
    public void Link(ProcessContext source, Pid target)
    {
        lock (gate)
        {
            RequireAlive(source.Self);
            if (processes.TryGetValue(target, out var p))
            {
                source.Links.Add(target);
                p.Links.Add(source.Self);
            }
            else
                Signal(source, target, Term.A("noproc"), false);
        }
    }
    public void Unlink(ProcessContext source, Pid target)
    {
        lock (gate)
        {
            source.Links.Remove(target);
            if (processes.TryGetValue(target, out var p))
                p.Links.Remove(source.Self);
        }
    }
    public ReferenceTerm Monitor(ProcessContext observer, Pid target)
    {
        lock (gate)
        {
            RequireAlive(observer.Self);
            var reference = new ReferenceTerm(Node, (ulong)++nextRef);
            if (processes.ContainsKey(target))
                monitors.Add(reference, (observer.Self, target));
            else
                observer.Mailbox.Send(Term.Tuple(Term.A("DOWN"), reference, Term.A("process"), target, Term.A("noproc")));
            return reference;
        }
    }
    public bool Demonitor(ProcessContext observer, ReferenceTerm reference, bool flush = false)
    {
        lock (gate)
        {
            if (monitors.TryGetValue(reference, out var m) && m.Observer.Equals(observer.Self))
                monitors.Remove(reference);
            if (flush)
                observer.Mailbox.Remove(t => t is TupleTerm x && x.Items.Count == 5 && x.Items[0].Equals(Term.A("DOWN")) && x.Items[1].Equals(reference));
            return true;
        }
    }
    public void Exit(ProcessContext sender, Pid target, Term reason)
    {
        lock (gate)
        {
            if (processes.TryGetValue(target, out var p))
                Signal(p, sender.Self, reason, true);
        }
    }
    private void Signal(ProcessContext target, Pid sender, Term reason, bool explicitSignal)
    {
        bool kill = explicitSignal && reason.Equals(Term.A("kill"));
        if (target.TrapExits && !kill)
        {
            target.Mailbox.Send(Term.Tuple(Term.A("EXIT"), sender, reason));
            return;
        }
        if (!kill && reason.Equals(Term.A("normal")) && !sender.Equals(target.Self))
            return;
        Terminate(target, kill ? Term.A("killed") : reason);
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
                ctx.Terminated = true;
                ctx.ExitReason = reason;
                foreach (var name in names.Where(p => p.Value.Equals(ctx.Self)).Select(p => p.Key).ToArray())
                    names.Remove(name);
                var links = ctx.Links.ToArray();
                ctx.Links.Clear();
                foreach (var m in monitors.ToArray())
                {
                    if (m.Value.Target.Equals(ctx.Self))
                    {
                        monitors.Remove(m.Key);
                        if (processes.TryGetValue(m.Value.Observer, out var observer))
                            observer.Mailbox.Send(Term.Tuple(Term.A("DOWN"), m.Key, Term.A("process"), ctx.Self, reason));
                    }
                    else if (m.Value.Observer.Equals(ctx.Self))
                        monitors.Remove(m.Key);
                }
                ctx.Stop.Cancel();
                ctx.Done.TrySetResult(reason);
                foreach (var pid in links)
                    if (processes.TryGetValue(pid, out var linked))
                    {
                        linked.Links.Remove(ctx.Self);
                        if (linked.TrapExits)
                            linked.Mailbox.Send(Term.Tuple(Term.A("EXIT"), ctx.Self, reason));
                        else if (!reason.Equals(Term.A("normal")))
                            pending.Enqueue((linked, reason));
                    }
            }
        }
    }
    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            disposed = true;
            foreach (var p in processes.Values.ToArray())
                Terminate(p, Term.A("shutdown"));
        }
        return ValueTask.CompletedTask;
    }
}
