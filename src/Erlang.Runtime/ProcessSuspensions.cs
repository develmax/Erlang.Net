namespace Erlang;

internal sealed class ProcessSuspensions(object gate, IReadOnlyDictionary<Pid, ProcessContext> processes, RuntimeTimers timers)
{
    private readonly Dictionary<Pid, Target> targets = new();

    public bool IsActive(Pid pid)
    {
        lock (gate)
            return targets.TryGetValue(pid, out var target) && target.Active;
    }

    private sealed class Owner(ProcessContext context)
    {
        public ProcessContext Context { get; } = context;
        public long Count = 1;
        public TaskCompletionSource<Atom> Acknowledged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<Term> Tags { get; } = new();
    }

    private sealed class Target(ProcessContext context)
    {
        public ProcessContext Context { get; } = context;
        public Dictionary<Pid, Owner> Owners { get; } = new();
        public bool Active;
        public TaskCompletionSource Resumed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async ValueTask<Term> Suspend(ProcessContext caller, Pid pid, ProcessSuspensionOptions options)
    {
        Task<Atom>? acknowledgement;
        bool result = true;
        lock (gate)
        {
            caller.Cancellation.ThrowIfCancellationRequested();
            if (!processes.TryGetValue(pid, out var context))
            {
                if (!options.Async)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (options.Tag is not null)
                    caller.Mailbox.SendTraced(
                        Term.Tuple(options.Tag, Term.A(ErlangErrorReasons.BadArgument)),
                        null,
                        preserve: true
                    );

                return Term.A(ErlangBooleanAtoms.True);
            }
            if (!targets.TryGetValue(pid, out var target))
            {
                target = new(context);
                targets.Add(pid, target);
                context.SuspensionRequested = true;
            }
            if (!target.Owners.TryGetValue(caller.Self, out var owner))
            {
                owner = new(caller);
                target.Owners.Add(caller.Self, owner);
            }
            else if (options.UnlessSuspending)
                result = false;
            else
            {
                if (owner.Count == ProcessSuspensionLimits.MaximumOwnerCount)
                    throw new ErlangException(ErlangErrorReasons.SystemLimit);
                owner.Count++;
            }
            if (options.Tag is not null)
                owner.Tags.Add(options.Tag);
            if (target.Active)
                Acknowledge(owner, Term.A(ProcessSuspensionNames.Suspended));
            context.Mailbox.Pulse();
            acknowledgement = options.Async ? null : owner.Acknowledged.Task;
        }
        if (acknowledgement is not null)
        {
            await caller.Mailbox.WaitForControlCompletion(acknowledgement, caller.Cancellation);
            Atom status = await acknowledgement;
            if (status.Name != ProcessSuspensionNames.Suspended)
                throw new ErlangException(status.Name == ProcessSuspensionNames.Exited ? ErlangErrorReasons.BadArgument : ProcessSuspensionNames.InternalError);
        }

        return Term.A(result ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);
    }

    public Term Resume(ProcessContext caller, Pid pid)
    {
        lock (gate)
        {
            if (!targets.TryGetValue(pid, out var target) || !target.Owners.TryGetValue(caller.Self, out var owner))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (--owner.Count == 0)
            {
                target.Owners.Remove(caller.Self);
                Acknowledge(owner, Term.A(ProcessSuspensionNames.NotSuspended));
                ReleaseIfEmpty(pid, target);
            }

            return Term.A(ErlangBooleanAtoms.True);
        }
    }

    public Task? EnterPause(ProcessContext context)
    {
        lock (gate)
        {
            if (!targets.TryGetValue(context.Self, out var target))
                return null;
            if (!target.Active)
            {
                target.Active = true;
                timers.PauseRecipient(context.Self);
            }
            foreach (var owner in target.Owners.Values)
                Acknowledge(owner, Term.A(ProcessSuspensionNames.Suspended));

            return target.Resumed.Task;
        }
    }

    public void Terminated(ProcessContext context)
    {
        lock (gate)
        {
            foreach (var pair in targets.ToArray())
            {
                if (pair.Key.Equals(context.Self))
                {
                    foreach (var owner in pair.Value.Owners.Values)
                        Acknowledge(owner, Term.A(ProcessSuspensionNames.Exited));
                    pair.Value.Owners.Clear();
                    ReleaseIfEmpty(pair.Key, pair.Value);
                }
                else if (pair.Value.Owners.Remove(context.Self, out var owner))
                {
                    Acknowledge(owner, Term.A(ProcessSuspensionNames.NotSuspended));
                    ReleaseIfEmpty(pair.Key, pair.Value);
                }
            }
        }
    }

    private static void Acknowledge(Owner owner, Atom result)
    {
        owner.Acknowledged.TrySetResult(result);
        foreach (Term tag in owner.Tags)
            owner.Context.Mailbox.SendTraced(Term.Tuple(tag, result), null, preserve: true);
        owner.Tags.Clear();
    }

    private void ReleaseIfEmpty(Pid pid, Target target)
    {
        if (target.Owners.Count != 0)
            return;
        targets.Remove(pid);
        target.Context.SuspensionRequested = false;
        if (target.Active)
            timers.ResumeRecipient(pid);
        target.Resumed.TrySetResult();
        target.Context.Mailbox.Pulse();
    }
}
