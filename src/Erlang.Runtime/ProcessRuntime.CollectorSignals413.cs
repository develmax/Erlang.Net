namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term CollectorCopyRequest413(
        ProcessContext requester,
        Term target,
        Term area,
        Term how
    )
    {
        lock (gate)
        {
            RequireCollectorRole413(requester, ManagedCollectorKind413.Literal);
            if (target is not Pid pid || !ReferenceEquals(pid.LocalIdentity, LocalIdentity)
                || how is not Atom { Name: ManagedCollectorNames413.Init or ManagedCollectorNames413.CheckGc or ManagedCollectorNames413.NeedGc })
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            CollectorOwnership413.RequireIndependentLiteralRoots();
            Term reply = Term.Tuple(Term.A(ManagedCollectorNames413.CopyLiterals),
                Term.Tuple(area, how, pid), Term.A(CommonAtomNames.Ok));
            if (!processes.TryGetValue(pid, out var recipient))
                SendSystem(requester.Self, reply); // Native dead-target request also completes ok.
            else
                EnqueueCollectorRequest413(
                    requester,
                    recipient,
                    reply,
                    false
                );

            return Term.A(CommonAtomNames.Ok);
        }
    }

    internal Term CollectorTraceSignal413(ProcessContext requester, Term target)
    {
        lock (gate)
        {
            RequireCollectorRole413(requester, ManagedCollectorKind413.Trace);
            CollectorOwnership413.RequireIndependentTraceRoots425();
            if (target is Pid pid && ReferenceEquals(pid.LocalIdentity, LocalIdentity))
            {
                if (!processes.TryGetValue(pid, out var recipient)) return CoreModules.Boolean(false);
                EnqueueCollectorRequest413(
                    requester,
                    recipient,
                    pid,
                    true
                );

                return CoreModules.Boolean(true);
            }
            if (target is PortTerm port && ReferenceEquals(port.LocalIdentity, LocalIdentity))
            {
                // Synchronous port lookup is real. Current legacy-only tracing has no
                // CLEARING session references in any port; no fake process ACK is emitted.
                _ = Ports.Find(port);

                return CoreModules.Boolean(false);
            }
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private void EnqueueCollectorRequest413(
        ProcessContext requester,
        ProcessContext recipient,
        Term reply,
        bool trace
    )
    {
        var request = new ManagedCollectorRequest413(
            requester,
            recipient,
            reply,
            trace
        );
        collectorRequests413.Add(request);
        try
        {
            recipient.Mailbox.SendPending(
                Nil.Value,
                () =>
            {
                // Same real runtime.AdmitSignals path as saved-call/info/code requests.
                // This runs after the target's preceding signals under runtime.gate.
                if (!System.Threading.Monitor.IsEntered(gate))
                    throw new InvalidOperationException(ManagedCollectorNames413.Gate);
                SettleCollectorRequest413(request, true);

                return null;
            },
                traceAdmission: false
            );
        }
        catch
        {
            collectorRequests413.Remove(request);
            request.Clear();
            throw;
        }
    }

    private void SettleCollectorRequest413(ManagedCollectorRequest413 request, bool reply)
    {
        if (!collectorRequests413.Remove(request)) return;
        ProcessContext? requester = request.Requester;
        Term? message = request.Reply;
        // Drop actual roots before any observing/mailbox callback can throw.
        request.Clear();
        if (reply && !collectorsClosed413 && requester is not null && message is not null
            && !requester.Terminated && processes.TryGetValue(requester.Self, out var live)
            && ReferenceEquals(live, requester))
            SendSystem(requester.Self, message);
    }
}
