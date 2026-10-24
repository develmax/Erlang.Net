namespace Erlang.Tests;

public static class ProcessInfoObservableHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                for (int round = 0; round < ProcessInfoObservableHostParameters.DeathRounds; round++)
                {
                    var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var target = runtime.Spawn(async child =>
                    {
                        using var frame = child.EnterFunction(
                            ProcessInfoObservableHostNames.Module,
                            ProcessInfoObservableHostNames.Function,
                            ProcessInfoObservableHostParameters.LogicalArity
                        );
                        child.Dictionary[Term.A(ProcessInfoObservableHostNames.Held)] = Term.I(round);
                        entered.SetResult();
                        await release.Task.WaitAsync(child.Cancellation);
                        await child.ReceiveAsync(message => message);

                        return Nil.Value;
                    });
                    await entered.Task.WaitAsync(ProcessInfoObservableHostParameters.Timeout);
                    ValueTask<Term> pending = Query(context, target.Pid, Term.A(ProcessInfoNames.Dictionary));
                    Require(!pending.IsCompleted);
                    if (round % 2 == 0)
                    {
                        ValueTask<Term> pendingStatus = Query(context, target.Pid, Term.A(ProcessInfoNames.Status));
                        Require(!pendingStatus.IsCompleted);
                        runtime.Exit(context, target.Pid, Term.A(ProcessExitSignals.Kill));
                        Require((await pending.AsTask().WaitAsync(ProcessInfoObservableHostParameters.Timeout)).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
                        Require((await pendingStatus.AsTask().WaitAsync(ProcessInfoObservableHostParameters.Timeout)).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
                    }
                    else
                    {
                        release.SetResult();
                        Require((await pending.AsTask().WaitAsync(ProcessInfoObservableHostParameters.Timeout)).Equals(Term.Tuple(Term.A(ProcessInfoNames.Dictionary), Term.List(Term.Tuple(Term.A(ProcessInfoObservableHostNames.Held), Term.I(round))))));
                        Term observed = await Query(
                            context,
                            target.Pid,
                            Term.List(Term.A(ProcessInfoNames.CurrentFunction), Term.A(ProcessInfoNames.Messages), Term.A(ProcessInfoNames.MessageQueueLength))
                        );
                        Require(observed.Equals(Term.List(
                            Term.Tuple(
                                Term.A(ProcessInfoNames.CurrentFunction),
                                Term.Tuple(
                                    Term.A(ProcessInfoObservableHostNames.Module),
                                    Term.A(ProcessInfoObservableHostNames.Function),
                                    Term.I(ProcessInfoObservableHostParameters.LogicalArity)
                                )
                            ),
                            Term.Tuple(Term.A(ProcessInfoNames.Messages), Nil.Value),
                            Term.Tuple(Term.A(ProcessInfoNames.MessageQueueLength), Term.I(0))
                        )));
                        runtime.Send(target.Pid, Nil.Value);
                    }
                    await target.Completion.WaitAsync(ProcessInfoObservableHostParameters.Timeout);
                    Require(context.Mailbox.Count == 0);
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(ProcessInfoObservableHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static ValueTask<Term> Query(ProcessContext context, Pid target, Term keys) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ProcessInfoNames.Function,
        target,
        keys
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessInfoObservableHostNames.Failure);
    }
}
