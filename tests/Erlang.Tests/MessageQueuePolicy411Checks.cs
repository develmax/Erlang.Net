namespace Erlang.Tests;

internal static class MessageQueuePolicy411Checks
{
    private static Term Option(Term mode) => Term.Tuple(Term.A(MessageQueuePolicy411Names.Flag), mode);

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            function,
            arguments
        );

    private static ValueTask<Term> Change(ProcessContext context, string mode)
        => Call(
            context,
            ErlangModuleNames.ProcessFlag,
            Term.A(MessageQueuePolicy411Names.Flag),
            Term.A(mode)
        );

    private static Term Info(string mode) => Term.Tuple(Term.A(MessageQueuePolicy411Names.Flag), Term.A(mode));

    internal static async Task Flags()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Require((await Call(context, MessageQueuePolicy411Names.SystemInfo, Term.A(MessageQueuePolicy411Names.Flag))).Equals(Term.A(MessageQueuePolicy411Names.On)));
            Require((await Call(
                context,
                MessageQueuePolicy411Names.Info,
                context.Self,
                Term.A(MessageQueuePolicy411Names.Flag)
            )).Equals(Info(MessageQueuePolicy411Names.On)));
            runtime.Send(context.Self, Term.A(MessageQueuePolicy411Names.First));
            runtime.Send(context.Self, Term.A(MessageQueuePolicy411Names.Second));
            Require((await Change(context, MessageQueuePolicy411Names.Off)).Equals(Term.A(MessageQueuePolicy411Names.On)));
            Require((await Change(context, MessageQueuePolicy411Names.Off)).Equals(Term.A(MessageQueuePolicy411Names.Off)));
            Require((await context.ReceiveAsync<Term>(value => value.Equals(Term.A(MessageQueuePolicy411Names.Second)) ? value : null, MessageQueuePolicy411Names.Deadline))!.Equals(Term.A(MessageQueuePolicy411Names.Second)));
            Require((await Change(context, MessageQueuePolicy411Names.On)).Equals(Term.A(MessageQueuePolicy411Names.Off)));
            Require((await context.ReceiveAsync<Term>(value => value, MessageQueuePolicy411Names.Deadline))!.Equals(Term.A(MessageQueuePolicy411Names.First)));
            Require((await Call(
                context,
                MessageQueuePolicy411Names.Info,
                context.Self,
                Term.A(MessageQueuePolicy411Names.Flag)
            )).Equals(Info(MessageQueuePolicy411Names.On)));

            return true;
        });
    }

    internal static async Task Spawn()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async owner =>
        {
            _ = await Change(owner, MessageQueuePolicy411Names.Off);
            foreach (bool request in new[] { false, true })
            {
                await Child(
                    owner,
                    request,
                    Nil.Value,
                    MessageQueuePolicy411Names.On
                );
                await Child(
                    owner,
                    request,
                    Term.List(Option(Term.A(MessageQueuePolicy411Names.Off))),
                    MessageQueuePolicy411Names.Off
                );
                await Child(
                    owner,
                    request,
                    Term.List(Option(Term.A(MessageQueuePolicy411Names.Off)), Option(Term.A(MessageQueuePolicy411Names.On))),
                    MessageQueuePolicy411Names.On
                );
            }

            return true;
        });
    }

    private static async Task Child(
        ProcessContext owner,
        bool request,
        Term options,
        string expected
    )
    {
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwind = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var function = new FunctionTerm(
            0,
            async (execution, _) =>
        {
            var child = (ProcessContext)execution;
            entered.TrySetResult(child);
            try
            {
                first.TrySetResult(await Call(
                    child,
                    MessageQueuePolicy411Names.Info,
                    child.Self,
                    Term.A(MessageQueuePolicy411Names.Flag)
                ));
                await release.Task.WaitAsync(child.Cancellation);

                return Nil.Value;
            }
            catch (Exception failure)
            {
                first.TrySetException(failure);
                throw;
            }
            finally
            {
                unwind.TrySetResult();
            }
        }
        );
        ProcessContext? target = null;
        ReferenceTerm? reference = null;
        try
        {
            Term result = await Call(
                owner,
                request ? MessageQueuePolicy411Names.Request : MessageQueuePolicy411Names.Spawn,
                function,
                Cons.From(Cons.Items(options).Append(Term.A(MessageQueuePolicy411Names.Monitor)))
            );
            reference = request ? (ReferenceTerm)result : (ReferenceTerm)((TupleTerm)result).Items[1];
            target = await entered.Task.WaitAsync(MessageQueuePolicy411Names.Deadline);
            Require((await first.Task.WaitAsync(MessageQueuePolicy411Names.Deadline)).Equals(Info(expected)) && !unwind.Task.IsCompleted);
            Require((await Call(
                owner,
                MessageQueuePolicy411Names.Info,
                target.Self,
                Term.A(MessageQueuePolicy411Names.Flag)
            )).Equals(Info(expected)));
            if (request)
            {
                Term? reply = await owner.ReceiveAsync<Term>(
                    term => term is TupleTerm { Items.Count: 4 } tuple
                    && tuple.Items[0].Equals(Term.A(MessageQueuePolicy411Names.Reply)) && tuple.Items[1].Equals(reference) ? term : null,
                    MessageQueuePolicy411Names.Deadline
                );
                Require(reply!.Equals(Term.Tuple(
                    Term.A(MessageQueuePolicy411Names.Reply),
                    reference,
                    Term.A(MessageQueuePolicy411Names.Success),
                    target.Self
                )));
            }
        }
        finally
        {
            release.TrySetResult();
            if (target is not null)
            {
                await unwind.Task.WaitAsync(MessageQueuePolicy411Names.Deadline);
                Term? down = await owner.ReceiveAsync<Term>(
                    term => term is TupleTerm { Items.Count: 5 } tuple
                    && tuple.Items[0].Equals(Term.A(MessageQueuePolicy411Names.Down)) && tuple.Items[1].Equals(reference) ? term : null,
                    MessageQueuePolicy411Names.Deadline
                );
                Require(down!.Equals(Term.Tuple(
                    Term.A(MessageQueuePolicy411Names.Down),
                    reference!,
                    Term.A(ProcessMonitorKinds.Process),
                    target.Self,
                    Term.A(ProcessExitReasons.Normal)
                )));
                Require(!owner.Runtime.IsAlive(target.Self));
            }
        }
    }

    internal static async Task Invalid()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async owner =>
        {
            foreach (Term value in new Term[] { Term.I(1), Term.A(MessageQueuePolicy411Names.Invalid), Nil.Value })
            {
                bool refused = false;
                try
                {
                    _ = await Call(
                        owner,
                        ErlangModuleNames.ProcessFlag,
                        Term.A(MessageQueuePolicy411Names.Flag),
                        value
                    );
                }
                catch (ErlangException error)
                {
                    refused = error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                Require(refused);
            }
            int ran = 0;
            var callback = new FunctionTerm(0, (_, _) =>
 {
     Interlocked.Increment(ref ran);

     return ValueTask.FromResult<Term>(Nil.Value);
 });
            foreach (Term options in new Term[] {
                Term.List(Option(Term.I(1))), Term.List(Option(Term.A(MessageQueuePolicy411Names.Invalid)), Option(Term.A(MessageQueuePolicy411Names.Off))),
                Term.List(Option(Term.A(MessageQueuePolicy411Names.On)), Option(Term.I(1))),
                new Cons(Option(Term.A(MessageQueuePolicy411Names.On)), Term.I(1)) })
            {
                bool refused = false;
                try
                {
                    _ = await Call(
                        owner,
                        MessageQueuePolicy411Names.Spawn,
                        callback,
                        options
                    );
                }
                catch (ErlangException error)
                {
                    refused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                    var frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
                    Require(frame.Items[2].Equals(Term.List(callback, options)));
                    Require(frame.Items[0].Equals(Term.A(ErlangModuleNames.Module)) && frame.Items[1].Equals(Term.A(MessageQueuePolicy411Names.Spawn)));
                    Require(frame.Items[3].Equals(Term.List(Term.Tuple(
                        Term.A(MessageQueuePolicy411Names.ErrorInfo),
                        new MapTerm([
                        new KeyValuePair<Term, Term>(Term.A(ProcessSpawnOptionNames.ErrorModule), Term.A(ProcessSpawnOptionNames.ErrorFormatter)),
                        new KeyValuePair<Term, Term>(Term.A(ProcessSpawnOptionNames.Cause), Term.A(ProcessSpawnOptionNames.BadOption))
                    ])
                    ))));
                }
                Require(refused && ran == 0);
            }

            return true;
        });
        Require(MessageQueuePolicy411Access.RemoteMode(Term.List(Option(Term.A(MessageQueuePolicy411Names.On)), Option(Term.A(MessageQueuePolicy411Names.Off)))) == MessageQueuePolicy411Names.OffEnum);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(MessageQueuePolicy411Names.Error);
    }
}
