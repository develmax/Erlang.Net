namespace Erlang.Tests;

internal static class SequenceTraceHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopSwitching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var records = new System.Collections.Concurrent.ConcurrentBag<Term>();
        var failures = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        ProcessHandle Collector(int label) => runtime.Spawn(async context =>
        {
            try
            {
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.I(label)
                );
                while (true)
                {
                    Term? message = await context.ReceiveAsync<Term>(value => value, SequenceTraceHostParameters.Timeout);
                    if (message!.Equals(Term.A(SequenceTraceHostParameters.Stop)))
                        break;
                    Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label))).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), Term.I(label))));
                    records.Add(message);
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            return Nil.Value;
        });
        var left = Collector(SequenceTraceHostParameters.LeftTracerLabel);
        var right = Collector(SequenceTraceHostParameters.RightTracerLabel);
        var receiver = runtime.Spawn(async context =>
        {
            try
            {
                for (int index = 0; index < SequenceTraceHostParameters.Senders * SequenceTraceHostParameters.MessagesPerSender; index++)
                {
                    var payload = (TupleTerm)(await context.ReceiveAsync<Term>(value => value, SequenceTraceHostParameters.Timeout))!;
                    Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label))).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), payload.Items[0])));
                    Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Serial))).Equals(Term.Tuple(Term.A(SequenceTraceNames.Serial), Term.Tuple(Term.I(0), new Integer(((Integer)payload.Items[1]).Value + 1)))));
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            return Nil.Value;
        });
        var senders = Enumerable.Range(0, SequenceTraceHostParameters.Senders).Select(index => runtime.Spawn(async context =>
        {
            try
            {
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.I(index)
                );
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Send),
                    Term.A(ErlangBooleanAtoms.True)
                );
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Receive),
                    Term.A(ErlangBooleanAtoms.True)
                );
                await start.Task;
                for (int message = 0; message < SequenceTraceHostParameters.MessagesPerSender; message++)
                {
                    ErlangOperatorValues.Send(context, receiver.Pid, Term.Tuple(Term.I(index), Term.I(message)));
                    await Task.Yield();
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            return Nil.Value;
        })).ToArray();
        var switcher = runtime.Spawn(async context =>
        {
            try
            {
                Require((await Call(
                    context,
                    SequenceTraceNames.SystemFlag,
                    Term.A(SequenceTraceNames.SystemTracer),
                    left.Pid
                )).Equals(Term.A(ErlangBooleanAtoms.False)));
                start.SetResult();
                bool nextRight = true;
                while (!stopSwitching.Task.IsCompleted)
                {
                    Term old = await Call(
                        context,
                        SequenceTraceNames.SystemFlag,
                        Term.A(SequenceTraceNames.SystemTracer),
                        nextRight ? right.Pid : left.Pid
                    );
                    Require(old.Equals(left.Pid) || old.Equals(right.Pid));
                    nextRight = !nextRight;
                    await Task.Yield();
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
                start.TrySetResult();
            }

            return Nil.Value;
        });
        await Task.WhenAll(senders.Select(sender => sender.Completion).Append(receiver.Completion)).WaitAsync(SequenceTraceHostParameters.Timeout);
        stopSwitching.SetResult();
        await switcher.Completion.WaitAsync(SequenceTraceHostParameters.Timeout);
        runtime.Send(left.Pid, Term.A(SequenceTraceHostParameters.Stop));
        runtime.Send(right.Pid, Term.A(SequenceTraceHostParameters.Stop));
        await Task.WhenAll(left.Completion, right.Completion).WaitAsync(SequenceTraceHostParameters.Timeout);
        if (failures.TryTake(out var failure))
            throw failure;
        Require(records.Count == SequenceTraceHostParameters.Senders * SequenceTraceHostParameters.MessagesPerSender * 2);
        var unique = new HashSet<Term>();
        foreach (Term record in records)
        {
            Require(record is TupleTerm { Items: [Atom { Name: SequenceTraceNames.Message }, Integer, TupleTerm] });
            var notification = (TupleTerm)record;
            var details = (TupleTerm)notification.Items[2];
            Require(details.Items.Count == 5 && details.Items[0] is Atom { Name: SequenceTraceNames.Send or SequenceTraceNames.Receive });
            var payload = (TupleTerm)details.Items[4];
            int index = (int)((Integer)payload.Items[0]).Value;
            Require(notification.Items[1].Equals(payload.Items[0]));
            Require(details.Items[1].Equals(Term.Tuple(Term.I(0), new Integer(((Integer)payload.Items[1]).Value + 1))));
            Require(details.Items[2].Equals(senders[index].Pid) && details.Items[3].Equals(receiver.Pid));
            Require(unique.Add(Term.Tuple(details.Items[0], details.Items[2], payload)));
        }
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
            throw new InvalidOperationException(SequenceTraceHostParameters.Failure);
    }
}
