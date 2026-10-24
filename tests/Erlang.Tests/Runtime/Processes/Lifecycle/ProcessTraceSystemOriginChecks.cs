namespace Erlang.Tests;

internal static class ProcessTraceSystemOriginChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        var records = new System.Collections.Concurrent.ConcurrentQueue<TupleTerm>();
        var collector = runtime.Spawn(async context =>
        {
            for (int index = 0; index < ProcessTraceSystemOriginParameters.Events; index++)
                records.Enqueue((TupleTerm)(await context.ReceiveAsync<Term>(value => value, ProcessTraceHostParameters.Timeout))!);

            return Nil.Value;
        });
        var target = runtime.Spawn(async context =>
        {
            await context.ReceiveAsync<Term>(value => value, ProcessTraceHostParameters.Timeout);

            return Nil.Value;
        });
        var controller = runtime.Spawn(async context =>
        {
            Term spec = Cons.From([Term.Tuple(
                Term.A(ProcessTraceNames.Wildcard),
                Nil.Value,
                Cons.From([Term.Tuple(Term.A(MatchSpecNames.Message), Term.A(MatchSpecNames.Whole))])
            )]);
            await Call(
                context,
                ProcessTraceNames.Pattern,
                Term.A(ProcessTraceNames.Receive),
                spec
            );
            await Call(
                context,
                ProcessTraceNames.Configure,
                context.Self,
                CoreModules.Boolean(true),
                Cons.From([
                Term.A(ProcessTraceNames.Receive),
                Term.Tuple(Term.A(ProcessTraceNames.Tracer), collector.Pid)
            ])
            );
            var reference = runtime.MakeReference();
            await Call(
                context,
                ProcessGarbageCollectionNames.Function,
                target.Pid,
                Cons.From([
                Term.Tuple(Term.A(ProcessGarbageCollectionNames.AsyncOption), reference)
            ])
            );
            Term garbageReply = Term.Tuple(Term.A(ProcessGarbageCollectionNames.ReplyTag), reference, CoreModules.Boolean(true));
            await context.ReceiveAsync<Term>(value => value.Equals(garbageReply) ? value : null, ProcessTraceHostParameters.Timeout);
            await Call(
                context,
                ProcessTraceSystemOriginParameters.Timer,
                Term.I(0),
                context.Self,
                Term.A(ProcessTraceSystemOriginParameters.TimerMessage)
            );
            await context.ReceiveAsync<Term>(
                value => value.Equals(Term.A(ProcessTraceSystemOriginParameters.TimerMessage)) ? value : null,
                ProcessTraceHostParameters.Timeout
            );
            await Call(
                context,
                ErlangModuleNames.Monitor,
                Term.A(ProcessTraceSystemOriginParameters.Process),
                target.Pid
            );
            runtime.Send(target.Pid, Term.A(ProcessTraceSystemOriginParameters.Stop));
            await context.ReceiveAsync<Term>(value => value is TupleTerm { Items.Count: 5 } ? value : null, ProcessTraceHostParameters.Timeout);

            return Nil.Value;
        });
        if (!(await controller.Completion.WaitAsync(ProcessTraceHostParameters.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)))
            throw new InvalidOperationException(ProcessTraceSystemOriginParameters.Test);
        await collector.Completion.WaitAsync(ProcessTraceHostParameters.Timeout);
        var events = records.ToArray();
        if (events.Length != ProcessTraceSystemOriginParameters.Events)
            throw new InvalidOperationException(ProcessTraceSystemOriginParameters.Test);
        for (int index = 0; index < events.Length; index++)
        {
            Term origin = index == 1 ? Term.A(ProcessTraceNames.ClockService) : runtime.NodeAtom;
            Term sender = index == 1 ? Term.A(ProcessTraceNames.Undefined) : target.Pid;
            if (!events[index].Items[4].Equals(Cons.From([origin, sender, events[index].Items[3]])))
                throw new InvalidOperationException(ProcessTraceSystemOriginParameters.Test);
        }
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        function,
        arguments
    );
}
