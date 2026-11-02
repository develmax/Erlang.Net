using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal static class SuspensionAck173TraceChecks
{
    internal static async Task Run(bool owned)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        await using var requester = new RuntimeNodeFoundationHostActor(runtime);
        await using var collector = new RuntimeNodeFoundationHostActor(runtime);
        var dead = runtime.Spawn(_ => ValueTask.FromResult<Term>(Nil.Value));
        await dead.Completion.WaitAsync(Deadline);
        await Configure(
            runtime,
            requester,
            collector,
            Term.A(SuspensionAck172Names.System),
            Term.A(SuspensionAck172Names.Undefined)
        );
        Term missingAck = Term.Tuple(Term.A(SuspensionAck172Names.MissingTag), Term.A(SuspensionAck172Names.BadArgument));
        await requester.InvokeAsync(async context =>
        {
            Term result = await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SuspensionAck172Names.Suspend,
                dead.Pid,
                Term.List(Term.Tuple(Term.A(SuspensionAck172Names.Asynchronous), Term.A(SuspensionAck172Names.MissingTag)))
            );
            Check(result.Equals(Term.A(SuspensionAck172Names.True)));
            Term? ack = await context.ReceiveAsync<Term>(value => value.Equals(missingAck) ? value : null, Deadline);
            Check(ack is not null && ack.Equals(missingAck));

            return Nil.Value;
        });
        ProcessContext owner = await requester.Ready;
        await RequireTrace(
            collector,
            owner.Self,
            missingAck,
            Term.A(SuspensionAck172Names.System),
            Term.A(SuspensionAck172Names.Undefined)
        );

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = runtime.Spawn(async context =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(context.Cancellation);
            await context.ExecutionCheckpoint();

            return Nil.Value;
        });
        try
        {
            await entered.Task.WaitAsync(Deadline);
            await Configure(
                runtime,
                requester,
                collector,
                runtime.NodeAtom,
                target.Pid
            );
            Term liveAck = Term.Tuple(Term.A(SuspensionAck172Names.LiveTag), Term.A(SuspensionAck172Names.Suspended));
            await requester.InvokeAsync(async context =>
            {
                Term result = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SuspensionAck172Names.Suspend,
                    target.Pid,
                    Term.List(Term.Tuple(Term.A(SuspensionAck172Names.Asynchronous), Term.A(SuspensionAck172Names.LiveTag)))
                );
                Check(result.Equals(Term.A(SuspensionAck172Names.True)));
                release.TrySetResult();
                Term? ack = await context.ReceiveAsync<Term>(value => value.Equals(liveAck) ? value : null, Deadline);
                Check(ack is not null && ack.Equals(liveAck));
                await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    SuspensionAck172Names.Resume,
                    target.Pid
                );

                return Nil.Value;
            });
            await RequireTrace(
                collector,
                owner.Self,
                liveAck,
                runtime.NodeAtom,
                target.Pid
            );
            await target.Completion.WaitAsync(Deadline);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    private static Task<Term> Configure(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor requester,
        RuntimeNodeFoundationHostActor collector,
        Term sourceNode,
        Term sourcePid
    )
        => requester.InvokeAsync(async context =>
        {
            Term pattern = Term.List(Term.Tuple(
                Term.List(sourceNode, sourcePid, Term.A(SuspensionAck172Names.Wildcard)),
                Nil.Value,
                Term.List(Term.Tuple(Term.A(SuspensionAck172Names.Message), Term.A(SuspensionAck172Names.Whole)))
            ));
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SuspensionAck172Names.TracePattern,
                Term.A(SuspensionAck172Names.TraceReceive),
                pattern
            );
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SuspensionAck172Names.TraceConfigure,
                context.Self,
                Term.A(SuspensionAck172Names.True),
                Term.List(Term.A(SuspensionAck172Names.TraceReceive),
                    Term.Tuple(Term.A(SuspensionAck172Names.Tracer), collector.Process.Pid))
            );

            return (Term)Nil.Value;
        });

    private static async Task RequireTrace(
        RuntimeNodeFoundationHostActor collector,
        Pid receiver,
        Term ack,
        Term sourceNode,
        Term sourcePid
    )
    {
        Term expected = Term.Tuple(
            Term.A(SuspensionAck172Names.TraceConfigure),
            receiver,
            Term.A(SuspensionAck172Names.TraceReceive),
            ack,
            Term.List(sourceNode, sourcePid, ack)
        );
        await collector.InvokeAsync(async context =>
        {
            Term? trace = await context.ReceiveAsync<Term>(value => value, Deadline);
            Check(trace is not null && trace.Equals(expected));

            return Nil.Value;
        });
    }
}
