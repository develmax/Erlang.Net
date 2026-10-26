using Erlang;
using System.Globalization;
using static Erlang.Tests.NewConnection501CapacityNames;

namespace Erlang.Tests;

internal static class NewConnection501CapacityChecks
{
    internal static async Task DefaultRetainedCapacityAndCleanup()
    {
        var retained = new List<TupleTerm>();
        var runtime = new ProcessRuntime();
        await using (runtime)
        {
            await Initialize(runtime);
            Check(runtime.DistributionPending.MaximumNodes == int.MaxValue);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            var references = new HashSet<ReferenceTerm>();
            for (int index = 0; index < RetainedNodes; index++)
            {
                var handle = await Open(runtime, actor, Node(index));
                Check(handle.Items[1] is ResourceReferenceTerm
                {
                    ResourceKind: ReferenceResourceKind.DistributionEntry
                } reference && references.Add(reference));
                retained.Add(handle);
            }
            Check(retained.Count == RetainedNodes);
            Check(runtime.ReferenceResources.LiveResources >= RetainedNodes);
            for (int index = 0; index < retained.Count; index++)
            {
                using var lease = runtime.DistributionPending.Acquire(retained[index]);
                Check(lease.Snapshot().State == DistributionPendingState.Pending);
                Check(retained[index].Equals(await Open(runtime, actor, Node(index))));
                Check((await Abort(
                    runtime,
                    actor,
                    Node(index),
                    retained[index]
                )).Equals(NewConnection501CapacityAtoms.True));
                Check(lease.Snapshot().State == DistributionPendingState.Idle);
            }
            // Holding the old terms keeps real reference resources rooted during the reuse check.
            var next = await Open(runtime, actor, Node(RetainedNodes - 1));
            CheckNextGeneration(retained[^1], next);
            Check((await Abort(
                runtime,
                actor,
                Node(RetainedNodes - 1),
                retained[^1]
            )).Equals(NewConnection501CapacityAtoms.True));
            using var nextLease = runtime.DistributionPending.Acquire(next);
            Check(nextLease.Snapshot().State == DistributionPendingState.Pending);
            Check((await Abort(
                runtime,
                actor,
                Node(RetainedNodes - 1),
                next
            )).Equals(NewConnection501CapacityAtoms.True));
        }
        Check(runtime.ReferenceResources.LiveResources == 0);
        foreach (var handle in retained)
            RejectClosed(() => runtime.DistributionPending.Acquire(handle));
        GC.KeepAlive(retained);
    }

    internal static async Task ExplicitQuotaAndIsolation()
    {
        await using var limited = new ProcessRuntime();
        Check(limited.InitializeDistributionPending(ExplicitQuota).MaximumNodes == ExplicitQuota);
        await Initialize(limited);
        Check(limited.DistributionPending.MaximumNodes == ExplicitQuota);
        await using var limitedActor = new RuntimeNodeFoundationHostActor(limited);
        var first = await Open(limited, limitedActor, Node(0));
        var second = await Open(limited, limitedActor, Node(1));
        using var firstLease = limited.DistributionPending.Acquire(first);
        using var secondLease = limited.DistributionPending.Acquire(second);
        await RejectQuota(Open(limited, limitedActor, Node(ExplicitQuota)));
        Check(first.Equals(await Open(limited, limitedActor, Node(0))));
        Check((await Abort(
            limited,
            limitedActor,
            Node(0),
            first
        )).Equals(NewConnection501CapacityAtoms.True));
        Check(firstLease.Snapshot().State == DistributionPendingState.Idle);
        // The quota counts retained entry identities, including an aborted entry with a live handle.
        await RejectQuota(Open(limited, limitedActor, Node(ExplicitQuota)));
        var next = await Open(limited, limitedActor, Node(0));
        CheckNextGeneration(first, next);
        Check(secondLease.Snapshot().State == DistributionPendingState.Pending);

        await using var independent = new ProcessRuntime();
        await Initialize(independent);
        Check(independent.DistributionPending.MaximumNodes == int.MaxValue);
        await using var independentActor = new RuntimeNodeFoundationHostActor(independent);
        var independentHandles = new List<TupleTerm>();
        for (int index = 0; index <= ExplicitQuota; index++)
            independentHandles.Add(await Open(independent, independentActor, Node(index)));
        Check(!independentHandles[0].Items[1].Equals(first.Items[1]));
        Check((await Abort(
            limited,
            limitedActor,
            Node(0),
            next
        )).Equals(NewConnection501CapacityAtoms.True));
        Check((await Abort(
            limited,
            limitedActor,
            Node(1),
            second
        )).Equals(NewConnection501CapacityAtoms.True));
        for (int index = 0; index < independentHandles.Count; index++)
            Check((await Abort(
                independent,
                independentActor,
                Node(index),
                independentHandles[index]
            )).Equals(NewConnection501CapacityAtoms.True));
        GC.KeepAlive(first);
        GC.KeepAlive(second);
    }

    private static Atom Node(int index)
        => Term.A(NodePrefix + index.ToString(CultureInfo.InvariantCulture) + NodeSuffix);

    private static async Task Initialize(ProcessRuntime runtime)
    {
        await runtime.InitializeModulesAsync(_ => { });
        Check(runtime.Modules.HasExport(Module, NewConnection, 1));
        Check(runtime.Modules.HasExport(Module, AbortPending, 2));
    }

    private static async Task<TupleTerm> Open(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        Atom node
    )
    {
        Term handle = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            Module,
            NewConnection,
            node
        ));
        Check(handle is TupleTerm { Items.Count: 2 });

        return (TupleTerm)handle;
    }

    private static Task<Term> Abort(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        Atom node,
        Term handle
    )
        => actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            Module,
            AbortPending,
            node,
            handle
        ));

    private static void CheckNextGeneration(TupleTerm previous, TupleTerm next)
    {
        Check(previous.Items[1].Equals(next.Items[1]));
        Check(next.Items[0].Equals(Term.I(((uint)((Integer)previous.Items[0]).Value + 1) & ConnectionIdMask)));
    }

    private static async Task RejectQuota(Task<TupleTerm> operation)
    {
        try
        {
            await operation;
            throw new InvalidOperationException(Failure);
        }
        catch (ErlangException exception) when (exception.Reason.Equals(NewConnection501CapacityAtoms.SystemLimit)) { }
    }

    private static void RejectClosed(Action operation)
    {
        try
        {
            operation();
            throw new InvalidOperationException(Failure);
        }
        catch (ObjectDisposedException) { }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
