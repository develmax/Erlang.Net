namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<ProcessContext, ProcessSystemTaskOwner428> systemTaskOwners428 = new();

    internal Term RequestSystemTask428(ProcessContext caller, IReadOnlyList<Term> arguments)
    {
        lock (gate)
        {
            RequireBeamfileActor418(caller);
            int offset = arguments.Count == 4 ? 1 : 0;
            Term requester = offset == 0 ? caller.Self : arguments[0];
            if (arguments[offset] is not Pid pid || pid.Node != Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (pid.LocalIdentity is not null && !ReferenceEquals(pid.LocalIdentity, LocalIdentity))
                throw new InvalidOperationException(SystemTaskNames428.Foreign);
            ProcessSchedulingPriority priority = arguments[offset + 1] switch
            {
                Atom { Name: SystemTaskNames428.Maximum } => ProcessSchedulingPriority.Maximum,
                Atom { Name: SystemTaskNames428.High } => ProcessSchedulingPriority.High,
                Atom { Name: SystemTaskNames428.Normal } => ProcessSchedulingPriority.Normal,
                Atom { Name: SystemTaskNames428.Low } => ProcessSchedulingPriority.Low,
                Atom { Name: SystemTaskNames428.Inherit } => caller.SchedulingPriorityLevel,
                _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
            };
            if (arguments[offset + 2] is not TupleTerm { Items.Count: >= 2 and <= 4 } operation
                || operation.Items[0] is not Atom tag)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            string kind = tag.Name;
            if (kind == SystemTaskNames428.GarbageCollect)
            {
                if (operation.Items.Count < 3 || operation.Items[2] is not Atom { Name: SystemTaskNames428.Minor or SystemTaskNames428.Major })
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            else if (kind == SystemTaskNames428.CheckCode)
            {
                if (operation.Items.Count < 3 || operation.Items[2] is not Atom)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            else if (kind != SystemTaskNames428.Test) throw new ErlangException(ErlangErrorReasons.BadArgument);
            var request = new SystemTaskRequest428(
                requester,
                operation,
                kind,
                priority
            );
            // External own-node identifiers are valid target descriptors but a
            // different incarnation never aliases an actual current process.
            if (!IsCurrentLocal(pid) || !processes.TryGetValue(pid, out var target) || target.Terminated)
            {
                ReplySystemTask428(request, false);
                request.Clear();

                return Term.A(CommonAtomNames.Ok);
            }
            if (kind == SystemTaskNames428.CheckCode && dirtyNative419?.IsExecuting423(target) == true)
            {
                Term result = DispatchDirtySystemTask428(caller, target, request);
                request.Clear();

                return result;
            }
            if (!systemTaskOwners428.TryGetValue(target, out var owner))
            {
                owner = new();
                systemTaskOwners428.Add(target, owner);
            }
            if (owner.Closing) throw new InvalidOperationException(SystemTaskNames428.Foreign);
            owner.Pending.AddLast(request.PendingNode);
            try
            {
                if (kind != SystemTaskNames428.Test && requester is Pid requesterPid && requesterPid.Equals(caller.Self))
                {
                    target.Mailbox.SendPendingPriority428(
                        Nil.Value,
                        () =>
                    {
                        if (!owner.Closing && request.PendingNode.List is not null) ReadySystemTask428(target, owner, request);

                        return null;
                    },
                        priority,
                        caller.Self
                    );
                }
                else ReadySystemTask428(target, owner, request);
            }
            catch
            {
                if (request.PendingNode.List is not null) owner.Pending.Remove(request.PendingNode);
                if (request.ReadyNode.List is { } queue) queue.Remove(request.ReadyNode);
                request.Clear();
                UpdateSystemTaskPriority428(target, owner);
                throw;
            }

            return Term.A(CommonAtomNames.Ok);
        }
    }

    private void ReplySystemTask428(SystemTaskRequest428 request, bool result)
    {
        if (request.Requester is Pid requester && IsCurrentLocal(requester))
            SendSystem(requester, Term.Tuple(Term.A(request.Kind), request.Id!, CoreModules.Boolean(result)));
    }

    private Term DispatchDirtySystemTask428(ProcessContext caller, ProcessContext target, SystemTaskRequest428 request)
    {
        var handler = DirtySystemHandler428(request.Priority);
        if (ReferenceEquals(handler, caller)) return Term.A(SystemTaskNames428.DirtyExecution);
        SendSystem(
            handler.Self,
            Term.Tuple(
                request.Requester!,
                target.Self,
                ProcessPriorityValues.ToAtom(request.Priority),
                request.Operation!
            ),
            Term.A(SystemTaskNames428.System)
        );

        return Term.A(CommonAtomNames.Ok);
    }

    private void ReadySystemTask428(ProcessContext target, ProcessSystemTaskOwner428 owner, SystemTaskRequest428 request)
    {
        if (owner.Closing || request.PendingNode.List is null) return;
        owner.Ready[(int)request.Priority].AddLast(request.ReadyNode);
        UpdateSystemTaskPriority428(target, owner);
        ScheduleSystemTaskPump428(target, owner);
    }

    private void UpdateSystemTaskPriority428(ProcessContext target, ProcessSystemTaskOwner428 owner)
    {
        target.SetSystemTaskPriority428(owner.HighestPriority);
        actorDispatcher.PriorityChanged(target);
    }

    private void ScheduleSystemTaskPump428(ProcessContext target, ProcessSystemTaskOwner428 owner)
    {
        if (owner.PumpQueued || owner.Closing || owner.HighestPriority < 0) return;
        var scheduler = target.ActorTaskScheduler ?? throw new InvalidOperationException(SystemTaskNames428.Foreign);
        owner.PumpQueued = true;
        try
        {
            // Housekeeping must not borrow another process's source ExecutionContext.
            if (ExecutionContext.IsFlowSuppressed()) _ = scheduler.InvokeSystemTask428(() => PumpSystemTask428(target, owner));
            else using (ExecutionContext.SuppressFlow()) _ = scheduler.InvokeSystemTask428(() => PumpSystemTask428(target, owner));
        }
        catch
        {
            owner.PumpQueued = false;
            throw;
        }
    }

    private ValueTask<Term> PumpSystemTask428(ProcessContext target, ProcessSystemTaskOwner428 owner)
    {
        SystemTaskRequest428? request = null;
        lock (gate)
        {
            owner.PumpQueued = false;
            if (owner.Closing || target.Terminated) return ValueTask.FromResult<Term>(Nil.Value);
            for (int i = owner.Ready.Length - 1; i >= 0; i--)
                if (owner.Ready[i].First is { } first)
                {
                    request = first.Value;
                    owner.Ready[i].Remove(first);
                    owner.Active = request;
                    break;
                }
            if (request is null) return ValueTask.FromResult<Term>(Nil.Value);
            UpdateSystemTaskPriority428(target, owner);
            if (request.Kind == SystemTaskNames428.CheckCode && dirtyNative419?.IsExecuting423(target) == true)
            {
                try
                {
                    DispatchDirtySystemTask428(target, target, request);
                }
                catch (Exception error)
                {
                    owner.Failures.Add(error);
                    if (!target.Terminated) Terminate(target, Term.A(SystemTaskNames428.TaskFailure));
                }
                finally
                {
                    owner.Pending.Remove(request.PendingNode);
                    owner.Active = null;
                    request.Clear();
                    request.PhysicalDone.TrySetResult();
                    UpdateSystemTaskPriority428(target, owner);
                    ScheduleSystemTaskPump428(target, owner);
                }

                return ValueTask.FromResult<Term>(Nil.Value);
            }
        }
        bool result = false;
        Exception? failure = null;
        try
        {
            if (request.Kind == SystemTaskNames428.GarbageCollect)
            {
                bool major = request.Operation!.Items[2] is Atom { Name: SystemTaskNames428.Major };
                CollectAtProcessSafePoint(target, major);
                result = true;
            }
            else if (request.Kind == SystemTaskNames428.CheckCode)
                result = Modules.IsExecutingOldCode(target, ((Atom)request.Operation!.Items[2]).Name);
            else result = true; // Genuine TEST task is executed in this actual target turn.
        }
        catch (Exception error)
        {
            failure = error;
        }
        lock (gate)
        {
            try
            {
                if (failure is not null) owner.Failures.Add(failure);
                if (request.PendingNode.List is not null)
                {
                    owner.Pending.Remove(request.PendingNode);
                    ReplySystemTask428(request, failure is null && !target.Terminated && result);
                }
            }
            catch (Exception error)
            {
                failure = error;
                owner.Failures.Add(error);
            }
            finally
            {
                owner.Active = null;
                request.Clear();
                request.PhysicalDone.TrySetResult();
                UpdateSystemTaskPriority428(target, owner);
                if (failure is not null && !target.Terminated) Terminate(target, Term.A(SystemTaskNames428.TaskFailure));
                else ScheduleSystemTaskPump428(target, owner);
            }
        }

        return ValueTask.FromResult<Term>(Nil.Value);
    }
}
