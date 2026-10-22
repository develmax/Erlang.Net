namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly Dictionary<ProcessContext, HashSet<InternalGroupLeaderRequest425>> internalGroupLeaderRequests425 = new();

    internal void RequireInternalProcessActor425(ProcessContext context)
    {
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    internal async ValueTask<Term> InternalGroupLeader425(ProcessContext caller, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 2)
        {
            if (arguments[0] is not Pid leader || arguments[1] is not Pid target) return Term.A(ErlangErrorReasons.BadArgument);
            lock (gate)
            {
                if (leader.LocalIdentity is not null && !ReferenceEquals(leader.LocalIdentity, LocalIdentity))
                    throw new InvalidOperationException(InternalProcessNames425.Foreign);
                if (target.LocalIdentity is not null && !ReferenceEquals(target.LocalIdentity, LocalIdentity))
                    throw new InvalidOperationException(InternalProcessNames425.Foreign);
                if (IsCurrentLocal(target))
                {
                    if (!processes.TryGetValue(target, out var process) || process.Terminated) return Term.A(ErlangErrorReasons.BadArgument);
                    if (!ReferenceEquals(process, caller)) return Term.A(ErlangBooleanAtoms.False);
                    process.GroupLeader = leader;

                    return Term.A(ErlangBooleanAtoms.True);
                }
            }

            return CoreModules.Boolean(await SetGroupLeaderAsync(caller, leader, target));
        }
        lock (gate)
        {
            if (arguments[0] is not Pid leader || arguments[1] is not Pid target || !IsCurrentLocal(target)
                || arguments[2] is not ReferenceTerm reference || !IsCurrentLocal(reference))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (leader.LocalIdentity is not null && !ReferenceEquals(leader.LocalIdentity, LocalIdentity))
                throw new InvalidOperationException(InternalProcessNames425.Foreign);
            var request = new InternalGroupLeaderRequest425(caller.Self, reference, leader);
            if (!processes.TryGetValue(target, out var process) || process.Terminated)
            {
                ReplyInternalGroupLeader425(request, false);

                return Term.A(CommonAtomNames.Ok);
            }
            if (!internalGroupLeaderRequests425.TryGetValue(process, out var requests))
                internalGroupLeaderRequests425.Add(process, requests = new());
            requests.Add(request);
            try
            {
                process.Mailbox.SendPending(
                    Nil.Value,
                    () =>
                {
                    lock (gate)
                    {
                        if (requests.Remove(request))
                        {
                            if (requests.Count == 0) internalGroupLeaderRequests425.Remove(process);
                            bool alive = !process.Terminated;
                            if (alive) process.GroupLeader = request.Leader;
                            ReplyInternalGroupLeader425(request, alive);
                        }
                    }

                    return null;
                },
                    from: caller.Self,
                    traceAdmission: false
                );
            }
            catch
            {
                requests.Remove(request);
                if (requests.Count == 0) internalGroupLeaderRequests425.Remove(process);
                throw;
            }

            return Term.A(CommonAtomNames.Ok);
        }
    }

    private void ReplyInternalGroupLeader425(InternalGroupLeaderRequest425 request, bool alive)
        => SendSystem(request.Requester, Term.Tuple(request.Reference, Term.A(alive ? ErlangBooleanAtoms.True : ErlangErrorReasons.BadArgument)));

    private void FinishInternalGroupLeader425(ProcessContext target)
    {
        if (!internalGroupLeaderRequests425.Remove(target, out var requests)) return;
        List<Exception>? failures = null;
        foreach (var request in requests)
        {
            try
            {
                ReplyInternalGroupLeader425(request, false);
            }
            catch (Exception failure)
            {
                (failures ??= new()).Add(failure);
            }
        }
        if (failures is not null) throw new AggregateException(failures);
    }

    internal async ValueTask<Term> RequestSuspend425(ProcessContext caller, IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Pid target || !IsCurrentLocal(target) || target.Equals(caller.Self)) return Term.A(ErlangErrorReasons.BadArgument);
        ProcessSuspensionOptions options;
        try
        {
            options = ProcessSuspensionOptions.Parse(arguments[1]);
        }
        catch (ErlangException)
        {
            return Term.A(ProcessSuspensionNames.BadOption);
        }
        try
        {
            return await Suspensions.Suspend(
                caller,
                target,
                options,
                internalProtocol: true
            );
        }
        catch (ErlangException error)
        {
            return error.Reason;
        }
    }

    internal Term InternalSystemMonitor425(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Atom { Name: InternalProcessNames425.Legacy })
            return ProcessTracing.RefuseNamedMonitor425(arguments[0]);
        if (arguments.Count == 1) return SystemMonitoring.Get();
        lock (gate)
        {
            Term target = arguments[1];
            Term options = arguments[2];
            if (target is not Atom { Name: SystemMonitorNames.Undefined } && options is not Nil
                && (target is not Pid pid || !IsCurrentLocal(pid) || !processes.TryGetValue(pid, out var process) || process.Terminated))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            try
            {
                return SystemMonitoring.Configure(target, options);
            }
            catch (ErlangException error) when (error.Reason is TupleTerm { Items: [Atom { Name: SystemMonitorNames.Unsupported }, _] })
            {
                throw new NotSupportedException(InternalProcessNames425.PhysicalMonitor, error);
            }
        }
    }
}
