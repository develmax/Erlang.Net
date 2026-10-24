namespace Erlang.Tests;

internal static class DistributionPendingChecks
{
    internal static async Task Run()
    {
        await ShapeResourcesAndGeneration();
        await KernelControllerDeathAndReplacement();
        await KernelReleaseAndDeath();
        await HaltDisposalAndLeaseClosure();
    }

    private static async Task ShapeResourcesAndGeneration()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        var table = runtime.InitializeDistributionPending(DistributionPendingTestNames.MaximumNodes);
        Check(ReferenceEquals(table, runtime.DistributionPending));
        Reject(() => runtime.InitializeDistributionPending(DistributionPendingTestNames.MaximumNodes));
        Reject(() => table.BeginPending(Term.A(DistributionPendingTestNames.InvalidNode)));
        Reject(() => table.BeginPending(runtime.NodeAtom));
        var first = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        var repeated = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        Check(first.Equals(repeated));
        Check(first.Items[1] is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.DistributionEntry });
        using var lease = table.Acquire(first);
        Check(lease.Snapshot().State == DistributionPendingState.Pending);
        uint id = lease.Snapshot().ConnectionId;
        Check(id <= DistributionPendingTestNames.ConnectionIdMask);
        var reference = (ReferenceTerm)first.Items[1];
        Check(runtime.ReferenceResources.AcquireBinary(reference) is null);
        Check(runtime.ReferenceResources.AcquirePrepared(reference) is null);
        Reject(() => table.Acquire(Term.Tuple(first.Items[0], runtime.ReferenceResources.CreateBinary(ReadOnlyMemory<byte>.Empty))));
        var plain = new ReferenceTerm(reference.Node, reference.Words, reference.Creation);
        Reject(() => table.Acquire(Term.Tuple(first.Items[0], plain)));
        Reject(() => foreign.DistributionPending.Acquire(first));
        byte[] encoded = ExternalTermFormat.Encode(first);
        Term restored = ExternalTermFormat.DecodeOne(
            encoded,
            new EtfDecodeOptions
            {
                LocalNode = runtime.Node,
                LocalCreation = 0,
                ReferenceFactory = runtime.ReferenceResources.Rehydrate
            }
        ).Term;
        using var restoredLease = table.Acquire(restored);
        Check(restoredLease.Snapshot().ConnectionId == id);
        Term foreignCopy = ExternalTermFormat.DecodeOne(
            encoded,
            new EtfDecodeOptions
            {
                LocalNode = foreign.Node,
                LocalCreation = 0,
                ReferenceFactory = foreign.ReferenceResources.Rehydrate
            }
        ).Term;
        Reject(() => foreign.DistributionPending.Acquire(foreignCopy));
        using var highAlias = table.Acquire(Term.Tuple(Term.I(id + DistributionPendingTestNames.Uint32Modulus), reference));
        using var lowAlias = table.Acquire(Term.Tuple(Term.I(id - DistributionPendingTestNames.Uint32Modulus), reference));
        Check(highAlias.Snapshot().ConnectionId == id && lowAlias.Snapshot().ConnectionId == id);
        Reject(() => table.Acquire(Term.Tuple(Term.I(DistributionPendingTestNames.NativeSmallMaximum + 1), reference)));
        Check(table.AbortPending(first));
        Check(!table.AbortPending(first));
        Check(lease.Snapshot().State == DistributionPendingState.Idle);
        var next = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        Check(next.Items[1].Equals(reference));
        using var nextLease = table.Acquire(next);
        Check(nextLease.Snapshot().ConnectionId == ((id + 1) & DistributionPendingTestNames.ConnectionIdMask));
        Reject(() => lease.Snapshot());
        Reject(() => table.Acquire(first));
        _ = table.BeginPending(Term.A(DistributionPendingTestNames.SecondNode));
        Reject(() => table.BeginPending(Term.A(DistributionPendingTestNames.ThirdNode)));
        nextLease.Dispose();
        Reject(() => nextLease.Snapshot());
    }

    private static async Task KernelControllerDeathAndReplacement()
    {
        await using var runtime = new ProcessRuntime();
        var table = runtime.DistributionPending;
        await using var kernel = new HostDistributionTestActor(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        var handle = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        await controller.Invoke(context => Reject(() => table.ReserveControllerSetup(handle, context)));
        await kernel.Invoke(context =>
        {
            runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);
            table.ReserveKernelSetup(context);

            return true;
        });
        await kernel.Invoke(context => Reject(() => table.ReserveControllerSetup(handle, context)));
        await controller.Invoke(context =>
        {
            table.ReserveControllerSetup(handle, context);

            return true;
        });
        var second = table.BeginPending(Term.A(DistributionPendingTestNames.SecondNode));
        await controller.Invoke(context => Reject(() => table.ReserveControllerSetup(second, context)));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = controller.InvokeAsync(async context =>
        {
            entered.SetResult();
            await release.Task;

            return Reject(() => table.ReserveControllerSetup(handle, context));
        });
        try
        {
            await entered.Task.WaitAsync(DistributionPendingTestNames.Deadline);
            await kernel.Invoke(context =>
            {
                runtime.Exit(context, controller.Process.Pid, Term.A(DistributionPendingTestNames.Kill));

                return true;
            });
            await controller.Process.Completion.WaitAsync(DistributionPendingTestNames.Deadline);
            using var oldLease = table.Acquire(handle);
            Check(oldLease.Snapshot().State == DistributionPendingState.Idle && oldLease.Snapshot().ReservedController is null);
            await using var replacement = new HostDistributionTestActor(runtime);
            var newer = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
            await replacement.Invoke(context =>
            {
                table.ReserveControllerSetup(newer, context);

                return true;
            });
            Reject(() => table.AbortPending(handle));
            release.SetResult();
            Check(await held);
            using var newerLease = table.Acquire(newer);
            Check(newerLease.Snapshot().ReservedController!.Equals(replacement.Process.Pid));
        }
        finally
        {
            release.TrySetResult();
        }
    }

    private static async Task KernelReleaseAndDeath()
    {
        await using var runtime = new ProcessRuntime();
        var table = runtime.DistributionPending;
        await using var kernel = new HostDistributionTestActor(runtime);
        await using var controller = new HostDistributionTestActor(runtime);
        await kernel.Invoke(context =>
        {
            runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);
            table.ReserveKernelSetup(context);

            return true;
        });
        for (int index = 0; index < DistributionPendingTestNames.Repetitions; index++)
        {
            var handle = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
            await controller.Invoke(context =>
            {
                table.ReserveControllerSetup(handle, context);

                return true;
            });
            await kernel.Invoke(context =>
            {
                table.ReleaseKernelSetup(context);
                table.ReserveKernelSetup(context);

                return true;
            });
            using var lease = table.Acquire(handle);
            Check(lease.Snapshot().State == DistributionPendingState.Idle && lease.Snapshot().ReservedController is null);
        }
        var pending = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        await controller.Invoke(context =>
        {
            runtime.Exit(context, kernel.Process.Pid, Term.A(DistributionPendingTestNames.Kill));

            return true;
        });
        await kernel.Process.Completion.WaitAsync(DistributionPendingTestNames.Deadline);
        using var after = table.Acquire(pending);
        Check(after.Snapshot().State == DistributionPendingState.Idle && after.Snapshot().ReservedKernel is null);
    }

    private static async Task HaltDisposalAndLeaseClosure()
    {
        await using var runtime = new ProcessRuntime();
        var table = runtime.DistributionPending;
        var handle = table.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        byte[] saved = ExternalTermFormat.Encode(handle);
        using var lease = table.Acquire(handle);
        var halt = runtime.RequestImmediateHalt(0);
        Reject(() => lease.Snapshot());
        Reject(() => table.Acquire(handle));
        await halt.WaitAsync(DistributionPendingTestNames.Deadline);
        await runtime.HaltQuiescence.WaitAsync(DistributionPendingTestNames.Deadline);
        var fallback = (TupleTerm)ExternalTermFormat.DecodeOne(saved, new EtfDecodeOptions
        {
            LocalNode = runtime.Node,
            ReferenceFactory = runtime.ReferenceResources.Rehydrate
        }).Term;
        Check(fallback.Items[1] is ReferenceTerm and not ResourceReferenceTerm);
        await using var disposed = new ProcessRuntime();
        var disposedTable = disposed.DistributionPending;
        var disposedHandle = disposedTable.BeginPending(Term.A(DistributionPendingTestNames.FirstNode));
        using var disposedLease = disposedTable.Acquire(disposedHandle);
        await disposed.DisposeAsync();
        Reject(() => disposedLease.Snapshot());
        Reject(() => disposedTable.BeginPending(Term.A(DistributionPendingTestNames.SecondNode)));
    }

    private static bool Reject(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return true;
        }

        throw new Exception(DistributionPendingTestNames.Failure);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionPendingTestNames.Failure);
    }
}
