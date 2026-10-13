using System.Collections.Concurrent;
using System.Text;

namespace Erlang.Tests;

internal static class JointPlatformHostChecks
{
    public static async Task Run()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(streams);
        var records = new ConcurrentQueue<TupleTerm>();
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Pid? controllerId = null;
        Pid? cleanupOwnerId = null;
        ProcessHandle collector = runtime.Spawn(async context =>
        {
            while (true)
            {
                Term item = (await context.ReceiveAsync<Term>(value => value, JointPlatformHostNames.Timeout))!;
                if (item.Equals(Term.A(JointPlatformHostNames.Stop)))
                    break;
                records.Enqueue((TupleTerm)item);
                var all = records.ToArray();
                if (all.Count(IsTrace) >= JointPlatformHostNames.ExpectedTraceRecords &&
                    all.Count(value => IsMonitor(value, controllerId)) >= JointPlatformHostNames.ExpectedCallerQueueEdges &&
                    all.Count(value => IsMonitor(value, cleanupOwnerId)) >= JointPlatformHostNames.ExpectedCleanupQueueEdges)
                    complete.TrySetResult();
            }

            return Nil.Value;
        });
        var owners = new List<Pid>();
        Pid? trappedId = null;
        Exception? failure = null;
        ProcessHandle controller = runtime.Spawn(async context =>
        {
            await start.Task;
            try
            {
                Term specification = Term.List(Term.Tuple(
                    Term.A(ProcessTraceNames.Wildcard),
                    Nil.Value,
                    Term.List(Term.Tuple(Term.A(MatchSpecNames.Message), Term.A(MatchSpecNames.Whole)))
                ));
                await Call(
                    context,
                    ProcessTraceNames.Pattern,
                    Term.A(ProcessTraceNames.Receive),
                    specification
                );
                await Trace(
                    context,
                    context.Self,
                    ProcessTraceNames.Receive,
                    collector.Pid
                );
                await Call(
                    context,
                    SystemMonitorNames.Function,
                    collector.Pid,
                    Term.List(Term.Tuple(Term.A(SystemMonitorNames.LongQueue), Term.Tuple(Term.I(0), Term.I(1))))
                );
                await Call(
                    context,
                    SequenceTraceNames.Set,
                    Term.A(SequenceTraceNames.Label),
                    Term.A(JointPlatformHostNames.Label)
                );
                await Call(context, SequenceTraceNames.Print, Term.A(JointPlatformHostNames.Label));
                for (int round = 0; round < JointPlatformHostNames.ExpectedOwnerReplies; round++)
                {
                    var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    ProcessHandle target = runtime.Spawn(async child =>
                    {
                        using var frame = child.EnterFunction(JointPlatformHostNames.Module, JointPlatformHostNames.Held, 0);
                        entered.SetResult();
                        await release.Task.WaitAsync(child.Cancellation);
                        await child.ReceiveAsync<Term>(value => value, JointPlatformHostNames.Timeout);

                        return Nil.Value;
                    });
                    owners.Add(target.Pid);
                    if (round == 0)
                        cleanupOwnerId = target.Pid;
                    await entered.Task.WaitAsync(JointPlatformHostNames.Timeout);
                    await Trace(
                        context,
                        target.Pid,
                        ProcessTraceNames.Send,
                        collector.Pid
                    );
                    long before = error.Length;
                    ValueTask<Term> pending = Call(
                        context,
                        ProcessDisplayNames.Function,
                        target.Pid,
                        Term.A(ProcessDisplayNames.Backtrace)
                    );
                    Require(!pending.IsCompleted);
                    if (round == 0)
                    {
                        release.SetResult();
                        Require((await pending.AsTask().WaitAsync(JointPlatformHostNames.Timeout)).Equals(CoreModules.Boolean(true)));
                        string text = Encoding.UTF8.GetString(error.ToArray())[(int)before..];
                        Require(text.Contains(target.Pid.ToString(), StringComparison.Ordinal));
                        Require(text.Contains(JointPlatformHostNames.Module + ":" + JointPlatformHostNames.Held + "/0", StringComparison.Ordinal));
                        runtime.Send(target.Pid, Term.A(JointPlatformHostNames.Stop));
                    }
                    else
                    {
                        runtime.Exit(context, target.Pid, Term.A(ProcessExitSignals.Kill));
                        try
                        {
                            await pending.AsTask().WaitAsync(JointPlatformHostNames.Timeout);
                            Require(false);
                        }
                        catch (ErlangException exception)
                        {
                            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
                        }
                        Require(error.Length == before);
                    }
                    Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label))).Equals(Term.Tuple(Term.A(SequenceTraceNames.Label), Term.A(JointPlatformHostNames.Label))));
                    Require((await Call(context, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Serial))).Equals(Term.Tuple(Term.A(SequenceTraceNames.Serial), Term.Tuple(Term.I(0), Term.I(1)))));
                    await target.Completion.WaitAsync(JointPlatformHostNames.Timeout);
                }
                var trappedReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var trappedRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                ProcessHandle trapped = runtime.Spawn(async child =>
                {
                    await Call(
                        child,
                        ErlangModuleNames.ProcessFlag,
                        Term.A(JointPlatformHostNames.TrapExit),
                        CoreModules.Boolean(true)
                    );
                    await Call(
                        child,
                        SequenceTraceNames.Set,
                        Term.A(SequenceTraceNames.Label),
                        Term.A(JointPlatformHostNames.ExitLabel)
                    );
                    trappedReady.SetResult();
                    await trappedRelease.Task;
                    Term received = (await child.ReceiveAsync<Term>(value => value, JointPlatformHostNames.Timeout))!;
                    Require(received.Equals(Term.Tuple(Term.A(ProcessMessageTags.LinkedExit), context.Self, Term.A(JointPlatformHostNames.Stop))));
                    Require((await Call(child, SequenceTraceNames.Info, Term.A(SequenceTraceNames.Label))) is Nil);

                    return Nil.Value;
                });
                trappedId = trapped.Pid;
                await trappedReady.Task.WaitAsync(JointPlatformHostNames.Timeout);
                await Trace(
                    context,
                    trapped.Pid,
                    ProcessTraceNames.Receive,
                    collector.Pid
                );
                runtime.Exit(context, trapped.Pid, Term.A(JointPlatformHostNames.Stop));
                trappedRelease.SetResult();
                Require((await trapped.Completion.WaitAsync(JointPlatformHostNames.Timeout)).Equals(Term.A(ProcessExitReasons.Normal)));
                await complete.Task.WaitAsync(JointPlatformHostNames.Timeout);
                await Call(context, SystemMonitorNames.Function, Term.A(SystemMonitorNames.Undefined));
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            return Nil.Value;
        });
        controllerId = controller.Pid;
        start.SetResult();
        await controller.Completion.WaitAsync(JointPlatformHostNames.Timeout);
        runtime.Send(collector.Pid, Term.A(JointPlatformHostNames.Stop));
        await collector.Completion.WaitAsync(JointPlatformHostNames.Timeout);
        if (failure is not null)
            throw failure;
        var all = records.ToArray();
        TupleTerm[] traces = all.Where(IsTrace).ToArray();
        Require(traces.Length == JointPlatformHostNames.ExpectedTraceRecords);
        Require(!all.Any(value => value.Items.Count >= 3 && value.Items[0].Equals(Term.A(ProcessTraceNames.Message)) && value.Items[2].Equals(Term.A(ProcessTraceNames.Send))));
        for (int index = 0; index < owners.Count; index++)
        {
            var entry = traces.Single(value => value.Items[1].Equals(controller.Pid) && value.Items[4] is Cons && Cons.Items(value.Items[4]).ElementAt(1).Equals(owners[index]));
            Term[] origin = Cons.Items(entry.Items[4]).ToArray();
            Require(origin[0].Equals(runtime.NodeAtom));
            Require(origin[2].Equals(entry.Items[3]));
            Require(entry.Items[3] is TupleTerm { Items.Count: 2 } reply && reply.Items[0] is ReferenceTerm);
            Require(((TupleTerm)entry.Items[3]).Items[1].Equals(index == 0 ? CoreModules.Boolean(true) : Term.A(ErlangErrorReasons.BadArgument)));
        }
        var exit = traces.Single(value => value.Items[1].Equals(trappedId));
        Require(exit.Items[4].Equals(Term.List(runtime.NodeAtom, controller.Pid, exit.Items[3])));
        var edges = all.Where(value => IsMonitor(value, controller.Pid)).ToArray();
        Require(edges.Length == JointPlatformHostNames.ExpectedCallerQueueEdges);
        for (int index = 0; index < edges.Length; index++)
            Require(edges[index].Items[3].Equals(CoreModules.Boolean(index % 2 == 0)));
        var ownerEdges = all.Where(value => IsMonitor(value, owners[0])).ToArray();
        Require(ownerEdges.Length == JointPlatformHostNames.ExpectedCleanupQueueEdges);
        for (int index = 0; index < ownerEdges.Length; index++)
            Require(ownerEdges[index].Items[3].Equals(CoreModules.Boolean(index % 2 == 0)));
        Require(!all.Any(value => owners.Skip(1).Any(owner => IsMonitor(value, owner))));
        Require(output.Length == 0);
    }

    private static bool IsTrace(TupleTerm value) => value.Items.Count == 5 && value.Items[0].Equals(Term.A(ProcessTraceNames.Message));

    private static bool IsMonitor(TupleTerm value, Pid? process) => process is not null && value.Items.Count == 4 && value.Items[0].Equals(Term.A(SystemMonitorNames.Notification)) && value.Items[1].Equals(process);

    private static async ValueTask<Term> Trace(
        ProcessContext context,
        Pid process,
        string flag,
        Pid collector
    ) => await Call(
        context,
        ProcessTraceNames.Configure,
        process,
        CoreModules.Boolean(true),
        Term.List(Term.A(flag), Term.Tuple(Term.A(ProcessTraceNames.Tracer), collector))
    );

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments) => context.Runtime.Modules.Call(
        context,
        ErlangModuleNames.Module,
        function,
        arguments
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(JointPlatformHostNames.Test);
    }
}
