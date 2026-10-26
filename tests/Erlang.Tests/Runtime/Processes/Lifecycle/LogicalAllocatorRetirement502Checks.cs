using System.Collections;
using static Erlang.Tests.LogicalAllocatorRetirement502Names;

namespace Erlang.Tests;

internal static class LogicalAllocatorRetirement502Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapAllocatorGather434.Register(runtime, registry);
            RuntimeBootstrapProcessAlive425.Register(runtime, registry);
        });
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await Bind(runtime, observer);
        await Bind(runtime, target);
        ProcessContext child = await target.Ready;
        await target.Invoke(context =>
        {
            runtime.Register(Registered, context.Self);

            return true;
        });
        ReferenceTerm monitor = await observer.Invoke(context => runtime.Monitor(context, child.Self));
        object dispatcher = Get(runtime, Dispatcher);
        Check((int)Call(dispatcher, AssignedLane, child)! == AllocatorGatherHost434Names.CallerLane);
        Check((int)Call(dispatcher, AssignedLane, await observer.Ready)! == AllocatorGatherHost434Names.CallerLane);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        object barrier = Call(
            dispatcher,
            PrepareAuxiliary,
            (Action<int>)(lane =>
        {
            if (lane != AllocatorGatherHost434Names.HeldLane)
                return;
            entered.Set();
            Check(release.Wait(Deadline));
        })
        )!;
        Task barrierCompletion = (Task)Get(barrier, Completion);
        Task? physical = null;
        Task<Term>? query = null;
        Task? disposal = null;
        try
        {
            Call(barrier, Publish);
            Check(entered.Wait(Deadline));
            ReferenceTerm reference = runtime.MakeReference();
            Term admitted = await target.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                AllocatorGatherHost434Names.Module,
                AllocatorGatherHost434Names.Hist,
                Term.Tuple(
                    Term.A(AllocatorGatherHost434Names.Standard),
                    Term.I(0),
                    Term.I(HistogramWidth),
                    Term.I(HistogramStart),
                    Term.I(0),
                    reference
                )
            ));
            Check(admitted.Equals(Term.I(1)));
            object owner = Get(runtime, Owner);
            lock (Get(runtime, RuntimeGate))
            {
                object[] pending = ((IEnumerable)Get(owner, Pending)).Cast<object>().ToArray();
                Check(pending.Length == 1 && ReferenceEquals(Get(pending[0], Origin), child));
                physical = (Task)Get(Get(pending[0], PhysicalDone), TaskProperty);
                Task actualBatch = (Task)Get(Get(pending[0], Batch), Completion);
                Check(!physical.IsCompleted && !actualBatch.IsCompleted && !barrierCompletion.IsCompleted);
            }
            await observer.Invoke(context =>
            {
                runtime.Exit(context, child.Self, LogicalRetirement502Names.KillAtom);

                return true;
            });
            var queryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            query = observer.InvokeAsync(async context =>
            {
                queryStarted.TrySetResult();
                Term alive = await runtime.Modules.Call(
                    context,
                    ProcessLiveness499Names.Erlang,
                    ProcessLiveness499Names.Alive,
                    child.Self
                );
                Check(alive.Equals(LogicalRetirement502Names.FalseAtom));
                ReferenceTerm aliveReference = runtime.MakeReference();
                await runtime.Modules.Call(
                    context,
                    AllocatorGatherHost434Names.Module,
                    ProcessLiveness499Names.Alive,
                    child.Self,
                    aliveReference
                );
                TupleTerm? native = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(aliveReference) ? tuple : null,
                    Deadline,
                    context.Cancellation
                );
                Check(native is not null && native.Items[1].Equals(LogicalRetirement502Names.FalseAtom));
                Check(runtime.WhereIs(Registered).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
                TupleTerm? down = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(LogicalRetirement502Names.DownAtom) &&
                        tuple.Items[1].Equals(monitor) && tuple.Items[3].Equals(child.Self) ? tuple : null,
                    Deadline,
                    context.Cancellation
                );
                Check(down is not null && down.Items[4].Equals(LogicalRetirement502Names.KilledAtom));

                return alive;
            });
            await queryStarted.Task.WaitAsync(Deadline);
            await query.WaitAsync(Deadline);
            Check(!physical.IsCompleted && !target.Process.Completion.IsCompleted && !barrierCompletion.IsCompleted);
            disposal = runtime.DisposeAsync().AsTask();
            await Task.WhenAny(disposal, Task.Delay(ProcessLiveness499Names.PendingWindow));
            Check(!disposal.IsCompleted && !physical.IsCompleted && !target.Process.Completion.IsCompleted);
        }
        finally
        {
            release.Set();
            await barrierCompletion.WaitAsync(Deadline);
            if (physical is not null)
                await physical.WaitAsync(Deadline);
            if (query is not null)
                await query.WaitAsync(Deadline);
            if (disposal is not null)
                await disposal.WaitAsync(Deadline);
        }
        Check((await target.Process.Completion.WaitAsync(Deadline)).Equals(LogicalRetirement502Names.KilledAtom));
        Check((bool)Get(Get(runtime, Owner), Drained));
    }

    private static Task<Term> Bind(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            AllocatorGatherHost434Names.Runtime,
            AllocatorGatherHost434Names.ProcessFlag,
            Term.A(AllocatorGatherHost434Names.Scheduler),
            Term.I(AllocatorGatherHost434Names.CallerLane)
        ));

    private static object Get(object target, string name) => AllocatorGatherHost434Access.Get(target, name);

    private static object? Call(object target, string name, params object[] arguments)
        => AllocatorGatherHost434Access.Call(target, name, arguments);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
