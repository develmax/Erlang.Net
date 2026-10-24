namespace Erlang.Tests;

public static class ProcessGarbageCollectionHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Exception? failure = null;
        var owner = runtime.Spawn(async context =>
        {
            try
            {
                int majorBefore = GC.CollectionCount(GC.MaxGeneration);
                Require((await runtime.Modules.Call(context, ErlangModuleNames.Module, ProcessGarbageCollectionNames.Function)).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(GC.CollectionCount(GC.MaxGeneration) > majorBefore);
                int minorBefore = GC.CollectionCount(ProcessGarbageCollectionParameters.ManagedYoungGeneration);
                Require((await Collect(
                    context,
                    context.Self,
                    Term.List(Term.Tuple(Term.A(ProcessGarbageCollectionNames.TypeOption), Term.A(ProcessGarbageCollectionNames.Minor)))
                )).Equals(Term.A(ErlangBooleanAtoms.True)));
                Require(GC.CollectionCount(ProcessGarbageCollectionParameters.ManagedYoungGeneration) > minorBefore);
                for (int round = 0; round < ProcessGarbageCollectionHostParameters.DeathRounds; round++)
                {
                    var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var target = runtime.Spawn(async child =>
                    {
                        entered.SetResult();
                        await blocked.Task.WaitAsync(child.Cancellation);

                        return Nil.Value;
                    });
                    await entered.Task.WaitAsync(ProcessGarbageCollectionHostParameters.Timeout);
                    Term id = Term.A(ProcessGarbageCollectionHostNames.Request);
                    Term options = Term.List(Term.Tuple(Term.A(ProcessGarbageCollectionNames.AsyncOption), id));
                    Require((await Collect(context, target.Pid, options)).Equals(Term.A(ProcessGarbageCollectionNames.AsyncResult)));
                    Require((await Collect(context, target.Pid, options)).Equals(Term.A(ProcessGarbageCollectionNames.AsyncResult)));
                    ValueTask<Term> sync = Collect(context, target.Pid, Nil.Value);
                    Require(!sync.IsCompleted);
                    runtime.Exit(context, target.Pid, Term.A(ProcessExitSignals.Kill));
                    Require((await sync.AsTask().WaitAsync(ProcessGarbageCollectionHostParameters.Timeout)).Equals(Term.A(ErlangBooleanAtoms.False)));
                    for (int replyIndex = 0; replyIndex < 2; replyIndex++)
                    {
                        var reply = await context.ReceiveAsync(
                            message => message is TupleTerm { Items.Count: 3 } tuple && tuple.Items[1].Equals(id) ? tuple : null,
                            ProcessGarbageCollectionHostParameters.Timeout
                        );
                        Require(reply is not null && reply.Items[0].Equals(Term.A(ProcessGarbageCollectionNames.ReplyTag)) && reply.Items[2].Equals(Term.A(ErlangBooleanAtoms.False)));
                    }
                    await target.Completion.WaitAsync(ProcessGarbageCollectionHostParameters.Timeout);
                    Require(context.Mailbox.Count == 0);
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        await owner.Completion.WaitAsync(ProcessGarbageCollectionHostParameters.Timeout);
        if (failure is not null)
            throw failure;
    }

    private static ValueTask<Term> Collect(ProcessContext context, Pid target, Term options) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        ProcessGarbageCollectionNames.Function,
        target,
        options
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessGarbageCollectionHostNames.Failure);
    }
}
