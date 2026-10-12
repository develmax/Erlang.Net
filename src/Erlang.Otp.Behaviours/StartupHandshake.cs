namespace Erlang.Otp;

internal sealed class StartupHandshake(ProcessContext? parent)
{
    private readonly TaskCompletionSource<Term> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private ReferenceTerm? monitor;
    private int sent;

    public ProcessHandle Spawn(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> body, bool link)
    {
        if (parent is null)
            return runtime.Spawn(body);
        var spawned = runtime.SpawnMonitor(parent, body, link);
        monitor = spawned.Reference;

        return spawned.Process;
    }

    public void Report(ProcessContext child, Term outcome)
    {
        if (child.Cancellation.IsCancellationRequested || Interlocked.Exchange(ref sent, 1) != 0)
            return;
        if (parent is null)
        {
            ready.TrySetResult(outcome);

            return;
        }
        bool success = outcome is TupleTerm { Items.Count: 2 } pair && pair.Items[0] is Atom { Name: BehaviourAtoms.Ok };
        parent.Runtime.Send(parent.Self, Term.Tuple(success ? StartupHandshakeAtoms.Acknowledged : StartupHandshakeAtoms.Rejected, child.Self, outcome));
    }

    public void Fail(ProcessContext child, Exception exception)
    {
        if (parent is null)
        {
            ready.TrySetException(exception);

            return;
        }
        Term reason = exception is ErlangException erlang ? erlang.ExceptionClass == ErlangExceptionClasses.Exit ? erlang.Reason : Term.Tuple(
            erlang.ExceptionClass == ErlangExceptionClasses.Throw ? Term.Tuple(Term.A(ErlangErrorReasons.NoCatch), erlang.Reason) : erlang.Reason,
            erlang.StackTraceTerm
        ) : Term.Tuple(Term.A(ProcessExitReasons.ClrError), Term.String(exception.GetType().Name));
        Report(child, Term.Tuple(Term.A(BehaviourAtoms.Error), reason));
    }

    private bool IsDown(Term value, Pid child) => value is TupleTerm { Items.Count: StartupHandshakeLayout.DownFields } down && down.Items[0].Equals(StartupHandshakeAtoms.Down) && down.Items[1].Equals(monitor!) && down.Items[2].Equals(StartupHandshakeAtoms.Process) && down.Items[3].Equals(child);

    private void FlushExit(Pid child)
    {
        parent!.Runtime.Unlink(parent, child);
        parent.Mailbox.RemoveFirst(value => value is TupleTerm { Items.Count: StartupHandshakeLayout.ExitFields } exit && exit.Items[0].Equals(StartupHandshakeAtoms.Exit) && exit.Items[1].Equals(child));
    }

    public async ValueTask<Term> Wait(ProcessHandle child)
    {
        if (parent is null)
        {
            await Task.WhenAny(ready.Task, child.Completion);
            if (!ready.Task.IsCompleted)
                return Term.Tuple(Term.A(BehaviourAtoms.Error), await child.Completion);
            Term hostOutcome = await ready.Task;
            if (hostOutcome is not TupleTerm { Items.Count: 2 } success || success.Items[0] is not Atom { Name: BehaviourAtoms.Ok })
                await child.Completion;

            return hostOutcome;
        }
        TupleTerm response = (await parent.ReceiveAsync(value =>
            value is TupleTerm { Items.Count: StartupHandshakeLayout.ResponseFields } tuple &&
                (tuple.Items[0].Equals(StartupHandshakeAtoms.Acknowledged) || tuple.Items[0].Equals(StartupHandshakeAtoms.Rejected)) && tuple.Items[1].Equals(child.Pid) ? tuple :
            IsDown(value, child.Pid) ? (TupleTerm)value : null))!;
        if (response.Items[0].Equals(StartupHandshakeAtoms.Acknowledged))
        {
            parent.Runtime.Demonitor(parent, monitor!, flush: true);

            return response.Items[2];
        }
        FlushExit(child.Pid);
        if (response.Items.Count == StartupHandshakeLayout.DownFields)
            return Term.Tuple(Term.A(BehaviourAtoms.Error), response.Items[StartupHandshakeLayout.DownReasonIndex]);
        await parent.ReceiveAsync(value => IsDown(value, child.Pid) ? (TupleTerm)value : null);

        return response.Items[2];
    }
}
