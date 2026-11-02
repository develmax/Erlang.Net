namespace Erlang;

internal sealed class ProcessSuspensions(object gate, IReadOnlyDictionary<Pid, ProcessContext> processes, RuntimeTimers timers)
{
    private readonly Dictionary<Pid, Target> targets = new();

    public bool IsActive(Pid pid)
    {
        lock (gate)
            return targets.TryGetValue(pid, out var target) && target.Active;
    }

    internal Term SnapshotOwned(Pid owner)
    {
        lock (gate)
        {
            var entries = new List<Term>();
            foreach (var pair in targets)
            {
                if (!pair.Value.Owners.TryGetValue(owner, out var suspension))
                    continue;
                entries.Add(Term.Tuple(
                    pair.Key,
                    Term.I(pair.Value.Active ? suspension.Count : 0),
                    Term.I(pair.Value.Active ? 0 : suspension.Count)
                ));
            }

            return Cons.From(entries);
        }
    }

    private sealed class Owner(ProcessContext context)
    {
        public ProcessContext Context { get; } = context;
        public long Count = 1;
        public TaskCompletionSource<Atom> Acknowledged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<SuspensionAckTag172> Tags { get; } = new();
        public List<KeyValuePair<ReferenceTerm, bool>> NativeReplies { get; } = new();
    }

    private sealed class Target(ProcessContext context)
    {
        public ProcessContext Context { get; } = context;
        public Dictionary<Pid, Owner> Owners { get; } = new();
        public bool Active;
        public TaskCompletionSource Resumed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async ValueTask<Term> Suspend(
        ProcessContext caller,
        Pid pid,
        ProcessSuspensionOptions options,
        bool internalProtocol = false
    )
    {
        // Keep original portability validation ahead of cancellation, then
        // cancel before introduced native discovery or cleanup-pump work.
        PortableControlTerm826? portableTag = options.Tag is null ? null
            : PortableControlTerm826.Capture(options.Tag);
        caller.Cancellation.ThrowIfCancellationRequested();
        // Actual typed graph reservation is outside R while source inputs live.
        SuspensionAckTag172? tag = portableTag is null ? null
            : caller.Runtime.MailboxRoots593.PrepareSuspensionAck172(caller.Runtime, portableTag);
        bool installed = false;
        try
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
                    if (tag is not null)
                    {
                        tag.Admit172();
                        tag.Publish172(caller, Term.A(ErlangErrorReasons.BadArgument), null);
                    }

                    return Term.A(ErlangBooleanAtoms.True);
                }
                bool newTarget = !targets.TryGetValue(pid, out var target);
                target ??= new(context);
                bool newOwner = !target.Owners.TryGetValue(caller.Self, out var owner);
                owner ??= new(caller);
                if (!newOwner && options.UnlessSuspending) result = false;
                else if (!newOwner && owner.Count == ProcessSuspensionLimits.MaximumOwnerCount)
                    throw new ErlangException(ErlangErrorReasons.SystemLimit);
                if (tag is not null)
                {
                    owner.Tags.EnsureCapacity(checked(owner.Tags.Count + 1));
                    owner.Tags.Add(tag);
                    try
                    {
                        tag.Admit172();
                    }
                    catch
                    {
                        owner.Tags.Remove(tag);
                        throw;
                    }
                }
                try
                {
                    if (newOwner) target.Owners.Add(caller.Self, owner);
                    if (newTarget) targets.Add(pid, target);
                }
                catch
                {
                    if (tag is not null) owner.Tags.Remove(tag);
                    if (newOwner) target.Owners.Remove(caller.Self);
                    throw;
                }
                if (!newOwner && !options.UnlessSuspending) owner.Count++;
                if (newTarget) context.SuspensionRequested = true;
                installed = tag is not null;
                if (target.Active)
                    Acknowledge(owner, Term.A(ProcessSuspensionNames.Suspended), pid);
                context.Mailbox.Pulse();
                if (internalProtocol && !options.Async && !target.Active)
                {
                    ReferenceTerm reference = caller.Runtime.MakeReference();
                    owner.NativeReplies.Add(new(reference, result));

                    return reference;
                }
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
        finally
        {
            // Uninstalled reservation/partial admission is cancelled and joined
            // only after leaving R. Installed roots belong to the owner/ACK.
            if (!installed && tag is not null) await tag.DisposeAsync();
        }
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
                try
                {
                    Acknowledge(owner, Term.A(ProcessSuspensionNames.NotSuspended), pid);
                }
                finally
                {
                    ReleaseIfEmpty(pid, target);
                }
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
            Exception? first = null;
            foreach (var owner in target.Owners.Values)
                try
                {
                    Acknowledge(owner, Term.A(ProcessSuspensionNames.Suspended), context.Self);
                }
                catch (Exception failure)
                {
                    first ??= failure;
                }
            if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();

            return target.Resumed.Task;
        }
    }

    internal void StopForHalt()
    {
        lock (gate)
        {
            Exception? first = null;
            foreach (Target target in targets.Values)
            {
                foreach (Owner owner in target.Owners.Values)
                {
                    Exception? failure = CancelTags172(owner);
                    first ??= failure;
                    owner.NativeReplies.Clear();
                    owner.Acknowledged.TrySetCanceled();
                }
                target.Owners.Clear();
                target.Context.SuspensionRequested = false;
                target.Resumed.TrySetCanceled();
            }
            targets.Clear();
            if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
        }
    }

    public void Terminated(ProcessContext context)
    {
        lock (gate)
        {
            Exception? first = null;
            foreach (var pair in targets.ToArray())
            {
                if (pair.Key.Equals(context.Self))
                {
                    foreach (var owner in pair.Value.Owners.Values)
                        try
                        {
                            Acknowledge(owner, Term.A(ProcessSuspensionNames.Exited), pair.Key);
                        }
                        catch (Exception failure)
                        {
                            first ??= failure;
                        }
                    pair.Value.Owners.Clear();
                    ReleaseIfEmpty(pair.Key, pair.Value);
                }
                else if (pair.Value.Owners.Remove(context.Self, out var owner))
                {
                    Exception? failure = CancelTags172(owner);
                    first ??= failure;
                    owner.NativeReplies.Clear();
                    owner.Acknowledged.TrySetCanceled();
                    ReleaseIfEmpty(pair.Key, pair.Value);
                }
            }
            if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
        }
    }

    private static void Acknowledge(Owner owner, Atom result, Pid from)
    {
        owner.Acknowledged.TrySetResult(result);
        Exception? first = null;
        foreach (SuspensionAckTag172 tag in owner.Tags)
        {
            try
            {
                tag.Publish172(owner.Context, result, from);
            }
            catch (Exception failure)
            {
                first ??= failure;
            }
            finally
            {
                try
                {
                    tag.Dispose();
                }
                catch (Exception failure)
                {
                    first ??= failure;
                }
            }
        }
        owner.Tags.Clear();
        foreach (var reply in owner.NativeReplies)
            try
            {
                owner.Context.Mailbox.SendTraced(
                    Term.Tuple(
                        reply.Key,
                        result.Name == ProcessSuspensionNames.Suspended
                        ? CoreModules.Boolean(reply.Value) : Term.A(result.Name == ProcessSuspensionNames.Exited
                            ? ErlangErrorReasons.BadArgument : ProcessSuspensionNames.InternalError)
                    ),
                    null,
                    preserve: true,
                    from: from
                );
            }
            catch (Exception failure)
            {
                first ??= failure;
            }
        owner.NativeReplies.Clear();
        if (first is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(first).Throw();
    }

    private static Exception? CancelTags172(Owner owner)
    {
        Exception? first = null;
        foreach (SuspensionAckTag172 tag in owner.Tags)
            try
            {
                tag.Dispose();
            }
            catch (Exception failure)
            {
                first ??= failure;
            }
        owner.Tags.Clear();

        return first;
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
