namespace Erlang.Tests;

internal static class ProcessTraceHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var records = new System.Collections.Concurrent.ConcurrentBag<Term>();
        var failures = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var collector = runtime.Spawn(async context =>
        {
            try
            {
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.I(ProcessTraceHostParameters.CollectorLabel)
                );
                for (int index = 0; index < ProcessTraceHostParameters.Senders * ProcessTraceHostParameters.MessagesPerSender * ProcessTraceHostParameters.EventsPerMessage; index++)
                {
                    Term record = (await context.ReceiveAsync<Term>(value => value, ProcessTraceHostParameters.Timeout))!;
                    records.Add(record);
                    Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label))).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), Term.I(ProcessTraceHostParameters.CollectorLabel))));
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            return Nil.Value;
        });
        var receiver = runtime.Spawn(async context =>
        {
            await Call(
                context,
                ProcessTraceNames.Configure,
                context.Self,
                CoreModules.Boolean(true),
                Cons.From([Term.A(ProcessTraceNames.Receive), Term.Tuple(Term.A(ProcessTraceNames.Tracer), collector.Pid)])
            );
            start.SetResult();
            await context.ReceiveAsync<Term>(value => value.Equals(Term.A(ProcessTraceHostParameters.Stop)) ? value : null, ProcessTraceHostParameters.Timeout);

            return Nil.Value;
        });
        var senders = Enumerable.Range(0, ProcessTraceHostParameters.Senders).Select(index => runtime.Spawn(async context =>
        {
            try
            {
                await start.Task;
                await Call(
                    context,
                    ProcessTraceNames.Configure,
                    context.Self,
                    CoreModules.Boolean(true),
                    Cons.From([Term.A(ProcessTraceNames.Send), Term.Tuple(Term.A(ProcessTraceNames.Tracer), collector.Pid)])
                );
                for (int message = 0; message < ProcessTraceHostParameters.MessagesPerSender; message++)
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        ErlangBasicBifNames.Send,
                        receiver.Pid,
                        Term.Tuple(Term.I(index), Term.I(message))
                    );
                Term reference = await Call(context, ProcessTraceNames.Delivered, context.Self);
                Term expected = Term.Tuple(Term.A(ProcessTraceNames.Delivered), context.Self, reference);
                Term? delivered = await context.ReceiveAsync<Term>(value => value.Equals(expected) ? value : null, ProcessTraceHostParameters.Timeout);
                Require(expected.Equals(delivered));
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            return Nil.Value;
        })).ToArray();
        await Task.WhenAll(senders.Select(sender => sender.Completion)).WaitAsync(ProcessTraceHostParameters.Timeout);
        await collector.Completion.WaitAsync(ProcessTraceHostParameters.Timeout);
        runtime.Send(receiver.Pid, Term.A(ProcessTraceHostParameters.Stop));
        await receiver.Completion.WaitAsync(ProcessTraceHostParameters.Timeout);
        if (!failures.IsEmpty)
            throw new AggregateException(failures);
        int expectedCount = ProcessTraceHostParameters.Senders * ProcessTraceHostParameters.MessagesPerSender;
        Term[] sent = records.OfType<TupleTerm>().Where(record => record.Items[2].Equals(Term.A(ProcessTraceNames.Send))).Select(record => record.Items[3]).ToArray();
        Term[] received = records.OfType<TupleTerm>().Where(record => record.Items[2].Equals(Term.A(ProcessTraceNames.Receive))).Select(record => record.Items[3]).ToArray();
        Require(sent.Length == expectedCount && received.Length == expectedCount && sent.Distinct().Count() == expectedCount && sent.All(received.Contains));
        await AsyncCompletion(false);
        await AsyncCompletion(true);
        await ProcessTraceSystemOriginChecks.Run();
    }

    private static async Task AsyncCompletion(bool fail)
    {
        await using var runtime = new ProcessRuntime();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var records = new System.Collections.Concurrent.ConcurrentQueue<Term>();
        runtime.Modules.Register(
            ProcessTraceHostParameters.Module,
            ProcessTraceHostParameters.Function,
            1,
            async (context, arguments) =>
        {
            entered.SetResult();
            await release.Task;
            if (fail)
                throw new ErlangException(ProcessTraceHostParameters.Failure);

            return arguments[0];
        }
        );
        var collector = runtime.Spawn(async context =>
        {
            while (true)
            {
                Term message = (await context.ReceiveAsync<Term>(value => value, ProcessTraceHostParameters.Timeout))!;
                if (message.Equals(Term.A(ProcessTraceHostParameters.Stop)))
                    break;
                if (message is TupleTerm { Items: [Atom { Name: ProcessTraceHostParameters.Snapshot }, Pid recipient] })
                    runtime.Send(recipient, Term.I(records.Count));
                else
                    records.Enqueue(message);
            }

            return Nil.Value;
        });
        var worker = runtime.Spawn(async context =>
        {
            await start.Task;
            try
            {
                await runtime.Modules.Call(
                    context,
                    ProcessTraceHostParameters.Module,
                    ProcessTraceHostParameters.Function,
                    Term.I(ProcessTraceHostParameters.CollectorLabel)
                );
            }
            catch (ErlangException exception) when (fail && exception.Reason.Equals(Term.A(ProcessTraceHostParameters.Failure)))
            {
            }

            return Nil.Value;
        });
        var controller = runtime.Spawn(async context =>
        {
            try
            {
                Term mfa = Term.Tuple(Term.A(ProcessTraceHostParameters.Module), Term.A(ProcessTraceHostParameters.Function), Term.I(1));
                Term specification = Cons.From([Term.Tuple(
                    Term.A(ProcessTraceNames.Wildcard),
                    Nil.Value,
                    Cons.From([Term.Tuple(Term.A(fail ? MatchSpecNames.ExceptionTrace : MatchSpecNames.ReturnTrace))])
                )]);
                Require((await Call(
                    context,
                    ProcessTraceNames.Pattern,
                    mfa,
                    specification
                )).Equals(Term.I(1)));
                await Call(
                    context,
                    ProcessTraceNames.Configure,
                    worker.Pid,
                    CoreModules.Boolean(true),
                    Cons.From([Term.A(ProcessTraceNames.Call), Term.Tuple(Term.A(ProcessTraceNames.Tracer), collector.Pid)])
                );
                start.SetResult();
                await entered.Task.WaitAsync(ProcessTraceHostParameters.Timeout);
                Term reference = await Call(context, ProcessTraceNames.Delivered, worker.Pid);
                Term notification = Term.Tuple(Term.A(ProcessTraceNames.Delivered), worker.Pid, reference);
                Require((await context.ReceiveAsync<Term>(value => value.Equals(notification) ? value : null, ProcessTraceHostParameters.Timeout))!.Equals(notification));
                runtime.Send(collector.Pid, Term.Tuple(Term.A(ProcessTraceHostParameters.Snapshot), context.Self));
                Require((await context.ReceiveAsync<Term>(value => value is Integer ? value : null, ProcessTraceHostParameters.Timeout))!.Equals(Term.I(1)));
                release.SetResult();
                await worker.Completion.WaitAsync(ProcessTraceHostParameters.Timeout);
            }
            finally
            {
                start.TrySetResult();
                release.TrySetResult();
                runtime.Send(collector.Pid, Term.A(ProcessTraceHostParameters.Stop));
            }

            return Nil.Value;
        });
        Require((await controller.Completion.WaitAsync(ProcessTraceHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
        await collector.Completion.WaitAsync(ProcessTraceHostParameters.Timeout);
        Term[] events = records.ToArray();
        Term expectedMfa = Term.Tuple(Term.A(ProcessTraceHostParameters.Module), Term.A(ProcessTraceHostParameters.Function), Term.I(1));
        Require(events.Length == ProcessTraceHostParameters.EventsPerMessage);
        Require(events[0] is TupleTerm { Items.Count: 4 } call && call.Items[2].Equals(Term.A(ProcessTraceNames.Call)));
        Term expectedResult = fail ? Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(ProcessTraceHostParameters.Failure)) : Term.I(ProcessTraceHostParameters.CollectorLabel);
        Require(events[1].Equals(Term.Tuple(
            Term.A(ProcessTraceNames.Message),
            worker.Pid,
            Term.A(fail ? ProcessTraceNames.Exception : ProcessTraceNames.Return),
            expectedMfa,
            expectedResult
        )));
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        function,
        arguments
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ProcessTraceHostParameters.Test);
    }
}
