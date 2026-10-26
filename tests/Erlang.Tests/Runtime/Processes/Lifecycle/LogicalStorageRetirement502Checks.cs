using System.Collections;
using System.Diagnostics;
using static Erlang.Tests.LogicalStorageRetirement502Names;

namespace Erlang.Tests;

internal static class LogicalStorageRetirement502Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(2);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapProcessAlive425.Register(runtime, registry));
        object storage = EtsFoundation405Access.Get(runtime);
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await using var target = new RuntimeNodeFoundationHostActor(runtime);
        await using var queryActor = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        await Bind(runtime, observer, QueryLane);
        await Bind(runtime, queryActor, QueryLane);
        await Bind(runtime, target, StorageLane);
        await Bind(runtime, killer, StorageLane);
        ProcessContext child = await target.Ready;
        object lease = await target.Invoke(context =>
        {
            runtime.Register(Registered, context.Self);
            Term table = EtsFoundation405Access.Create(storage, context);
            long slot = EtsFoundation405Access.Store(
                storage,
                context,
                table,
                Term.Tuple(Term.I(1))
            );

            return EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.AcquireRead,
                context,
                table,
                slot
            )!;
        });
        ReferenceTerm monitor = await observer.Invoke(context => runtime.Monitor(context, child.Self));
        object storedGate = Get(Get(lease, LeaseOwner), Gate);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        // Contend on the actual stored-object lock needed by ETS retirement's Release.
        // A read lease alone retains payload roots but does not delay logical table cleanup.
        Task heldStorage = Task.Factory.StartNew(
            () =>
        {
            lock (storedGate)
            {
                entered.Set();
                Check(release.Wait(Deadline));
            }
        },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        );
        Task<Term>? query = null;
        try
        {
            Check(entered.Wait(Deadline));
            ReferenceTerm nativeReference = runtime.MakeReference();
            await killer.Invoke(context =>
            {
                runtime.Exit(context, child.Self, LogicalRetirement502Names.KillAtom);

                return true;
            });
            await observer.InvokeAsync(async context =>
            {
                await runtime.Modules.Call(
                    context,
                    LogicalRetirement502Names.Internal,
                    ProcessLiveness499Names.Alive,
                    child.Self,
                    nativeReference
                );

                return true;
            });
            query = queryActor.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ProcessLiveness499Names.Erlang,
                ProcessLiveness499Names.Alive,
                child.Self
            ));
            var elapsed = Stopwatch.StartNew();
            while (!QueriesAdmitted(runtime, child))
            {
                Check(!query.IsCompleted && elapsed.Elapsed < Deadline);
                await Task.Delay(PollInterval);
            }
            Check(!heldStorage.IsCompleted && !query.IsCompleted && !target.Process.Completion.IsCompleted);
            Check(runtime.WhereIs(Registered).Equals(child.Self));
            await observer.InvokeAsync(async context =>
            {
                TupleTerm? native = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(nativeReference) ? tuple : null,
                    TimeSpan.Zero,
                    context.Cancellation
                );
                TupleTerm? down = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(LogicalRetirement502Names.DownAtom) &&
                        tuple.Items[1].Equals(monitor) ? tuple : null,
                    TimeSpan.Zero,
                    context.Cancellation
                );
                Check(native is null && down is null);

                return true;
            });
            release.Set();
            await heldStorage.WaitAsync(Deadline);
            Check((await query.WaitAsync(Deadline)).Equals(LogicalRetirement502Names.FalseAtom));
            await observer.InvokeAsync(async context =>
            {
                TupleTerm? native = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(nativeReference) ? tuple : null,
                    Deadline,
                    context.Cancellation
                );
                TupleTerm? down = await context.Mailbox.ReceiveAsync<TupleTerm>(
                    value => value is TupleTerm { Items.Count: 5 } tuple && tuple.Items[0].Equals(LogicalRetirement502Names.DownAtom) &&
                        tuple.Items[1].Equals(monitor) && tuple.Items[3].Equals(child.Self) ? tuple : null,
                    Deadline,
                    context.Cancellation
                );
                Check(native is not null && native.Items[1].Equals(LogicalRetirement502Names.FalseAtom));
                Check(down is not null && down.Items[4].Equals(LogicalRetirement502Names.KilledAtom));

                return true;
            });
            Check(runtime.WhereIs(Registered).Equals(Term.A(ProcessRegistryAtoms.Undefined)));
            Check((await target.Process.Completion.WaitAsync(Deadline)).Equals(LogicalRetirement502Names.KilledAtom));
        }
        finally
        {
            release.Set();
            await heldStorage.WaitAsync(Deadline);
            ((IDisposable)lease).Dispose();
            if (query is not null)
                await query.WaitAsync(Deadline);
        }
        EtsFoundation405Access.Empty(storage);
    }

    private static bool QueriesAdmitted(ProcessRuntime runtime, ProcessContext child)
    {
        lock (Get(runtime, Gate))
        {
            var exits = (IDictionary)Get(runtime, ExitContinuations);
            Check(exits.Contains(child));
            Check(!((Task)Get(exits[child]!, StorageRetirement)).IsCompleted);
            var requests = (IDictionary)Get(runtime, AliveRequests);

            return requests.Contains(child) && (int)Get(requests[child]!, Count) == AdmittedQueries;
        }
    }

    private static object Get(object target, string name) => AllocatorGatherHost434Access.Get(target, name);

    private static Task<Term> Bind(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, int lane)
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            AllocatorGatherHost434Names.Runtime,
            AllocatorGatherHost434Names.ProcessFlag,
            Term.A(AllocatorGatherHost434Names.Scheduler),
            Term.I(lane)
        ));

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
