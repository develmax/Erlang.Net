namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void ExitWithSequencePreparation(
        ProcessContext sender,
        Pid target,
        Term reason,
        RuntimeProcessPayload641 origin641
    )
    {
        if (TryExitImmediateWithoutSequencePreparation(
            sender,
            target,
            reason,
            origin641
        ))
            return;
        AssertSequenceTracePreparation();
        while (true)
        {
            // Retain the existing immediate trapping-message physical route.
            if (TryPublishBackedLocalExitAsync(sender, target, reason).GetAwaiter().GetResult()) return;
            ProcessContext victim;
            PendingSequenceTerminationControl terminal;
            lock (gate)
            {
                if (!processes.TryGetValue(target, out victim!)) return;
                bool kill = reason is Atom { Name: ProcessExitSignals.Kill };
                terminal = CreatePendingSequenceTerminationControl(
                    victim,
                    kill ? Term.A(ProcessExitReasons.Killed) : reason,
                    () => !victim.Terminated && (kill || !victim.TrapExits)
                        && (kill || reason is not Atom { Name: ProcessExitReasons.Normal } || sender.Self.Equals(victim.Self))
                );
            }
            try
            {
                terminal.PrepareOutsideRuntimeGate();
                lock (gate)
                {
                    if (!processes.TryGetValue(target, out ProcessContext? current) || !ReferenceEquals(current, victim)) return;
                    if (!terminal.IsCurrent()) continue;
                    Signal(
                        victim,
                        sender.Self,
                        reason,
                        true,
                        origin641,
                        terminal.Prepared
                    );

                    return;
                }
            }
            finally
            {
                terminal.ReleaseOutsideRuntimeGate();
            }
        }
    }

    // Legacy mailbox admission/release callbacks are synchronous under R/M.
    // This exact immediate kill has no payload or token to prepare:641
    // already owns the input, and absence of required fanout is checked under R.
    // Every case needing actual sequence roots retains the outside-R path.
    private bool TryExitImmediateWithoutSequencePreparation(
        ProcessContext sender,
        Pid target,
        Term reason,
        RuntimeProcessPayload641 origin641
    )
    {
        if (!System.Threading.Monitor.IsEntered(gate)
            || !ReferenceEquals(sender.Runtime, this)
            || target.Equals(InitializationPid)
            || reason is not Atom { Name: ProcessExitSignals.Kill })
            return false;
        lock (gate)
        {
            if (!processes.TryGetValue(target, out ProcessContext? current))
                return true;
            if (NeedsSequenceTerminationPreparation(current))
                return false;
            Signal(
                current,
                sender.Self,
                reason,
                true,
                origin641,
                null
            );

            return true;
        }
    }

    private void SendPreparedExitWithSequence(
        ProcessContext sender,
        Term destination,
        Term reason,
        bool legacy,
        bool priority,
        RuntimeProcessPayload641 complete641
    )
    {
        AssertSequenceTracePreparation();
        if (!legacy || destination is not Pid pid || !pid.Equals(sender.Self))
        {
            SendPreparedExitCore641(
                sender,
                destination,
                reason,
                legacy,
                priority,
                complete641,
                null
            );

            return;
        }
        while (true)
        {
            ProcessContext victim;
            PendingSequenceTerminationControl terminal;
            lock (gate)
            {
                if (!processes.TryGetValue(pid, out victim!)) return;
                bool kill = reason is Atom { Name: ProcessExitSignals.Kill };
                terminal = CreatePendingSequenceTerminationControl(
                    victim,
                    kill ? Term.A(ProcessExitReasons.Killed) : reason,
                    () => !victim.Terminated && (kill || !victim.TrapExits)
                );
            }
            try
            {
                terminal.PrepareOutsideRuntimeGate();
                lock (gate)
                {
                    if (!processes.TryGetValue(pid, out ProcessContext? current) || !ReferenceEquals(current, victim)) return;
                    if (!terminal.IsCurrent()) continue;
                    SendPreparedExitCore641(
                        sender,
                        destination,
                        reason,
                        legacy,
                        priority,
                        complete641,
                        terminal.Prepared
                    );

                    return;
                }
            }
            finally
            {
                terminal.ReleaseOutsideRuntimeGate();
            }
        }
    }
}
