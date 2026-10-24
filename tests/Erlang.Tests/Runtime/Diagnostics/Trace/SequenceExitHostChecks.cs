namespace Erlang.Tests;

internal static class SequenceExitHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var receiverReady = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseChildren = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var admitExits = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new HashSet<Pid>();
        ProcessHandle receiver = runtime.Spawn(async context =>
        {
            context.TrapExits = true;
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                Term.A(SequenceExitHostParameters.Parent)
            );
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(0), Term.I(SequenceExitHostParameters.ParentSerial))
            );
            receiverReady.SetResult(context);
            await admitExits.Task;
            for (int index = 0; index < SequenceExitHostParameters.Children; index++)
            {
                TupleTerm? message = await context.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items: [Atom { Name: ProcessMessageTags.LinkedExit }, Pid, Atom { Name: ProcessExitReasons.Normal }] } tuple ? tuple : null,
                    SequenceExitHostParameters.Timeout
                );
                Require(message is not null);
                Pid source = (Pid)message!.Items[1];
                Require(received.Add(source));
                Term token = await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Token),
                    Nil.Value
                );
                Require(token is TupleTerm { Items: [Integer { Value.IsZero: true }, Pid, Integer, Pid, Integer] });
                var fields = ((TupleTerm)token).Items;
                Require(fields[1].Equals(source) && fields[3].Equals(source));
                Require(fields[2].Equals(Term.I(SequenceExitHostParameters.ChildSerial + 1)));
                Require(fields[4].Equals(Term.I(SequenceExitHostParameters.ChildLastCount)));
            }
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                Term.A(SequenceExitHostParameters.AfterExits)
            );
            Require((await Call(context, SequenceTraceNames.Print, Term.A(SequenceExitHostParameters.Clock))).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Serial))).Equals(Term.Tuple(
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(SequenceExitHostParameters.ChildSerial + 1), Term.I(SequenceExitHostParameters.ParentSerial + 1))
            )));

            return Term.A(ErlangBooleanAtoms.True);
        });
        await receiverReady.Task.WaitAsync(SequenceExitHostParameters.Timeout);
        var ready = Enumerable.Range(0, SequenceExitHostParameters.Children).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        ProcessHandle[] children = Enumerable.Range(0, SequenceExitHostParameters.Children).Select(index => runtime.Spawn(async context =>
        {
            runtime.Link(context, receiver.Pid);
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Label),
                context.Self
            );
            await Call(
                context,
                SequenceTraceNames.Set,
                Term.A(SequenceTraceNames.Serial),
                Term.Tuple(Term.I(SequenceExitHostParameters.ChildLastCount), Term.I(SequenceExitHostParameters.ChildSerial))
            );
            ready[index].SetResult();
            await releaseChildren.Task;

            return Term.A(ErlangBooleanAtoms.True);
        })).ToArray();
        await Task.WhenAll(ready.Select(item => item.Task)).WaitAsync(SequenceExitHostParameters.Timeout);
        releaseChildren.SetResult();
        await Task.WhenAll(children.Select(child => child.Completion)).WaitAsync(SequenceExitHostParameters.Timeout);
        admitExits.SetResult();
        Require((await receiver.Completion.WaitAsync(SequenceExitHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
        Require(received.Count == SequenceExitHostParameters.Children);
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
            throw new InvalidOperationException(SequenceExitHostParameters.Failure);
    }
}
