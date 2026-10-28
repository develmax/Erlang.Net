using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang.Tests;

public static class NifAllocationDomainHostChecks
{
    private static readonly List<string> passed = new();
    internal static IReadOnlyList<string> Completed => passed.AsReadOnly();

    public static async Task Run()
    {
        passed.Clear();
        NifAllocationDomainHostBridge.Preflight();
        await using var runtime = new ProcessRuntime();
        var sourceOwner = new object();
        var descriptor = new ModuleCodeDescriptor(
            [(NifAllocationDomainHostNames.Function, 0)],
            [(NifAllocationDomainHostNames.Function, 0)],
            new BitString(new byte[MD5.HashSizeInBytes]),
            Nil.Value,
            sourceOwner: sourceOwner
        )
        {
            DeclaredNifs = [(NifAllocationDomainHostNames.Function, 0)]
        };
        runtime.Modules.RegisterSource(
            NifAllocationDomainHostNames.Module,
            NifAllocationDomainHostNames.Function,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value),
            sourceOwner
        );
        runtime.Modules.PublishModuleCode(NifAllocationDomainHostNames.Module, descriptor);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(NifAllocationDomainHostNames.Module)!;
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            await ConstructedAndCancelled(
                runtime,
                image,
                context,
                descriptor
            );
            await PayloadAndDisposal(
                runtime,
                image,
                context,
                descriptor
            );
            await GlobalPeakAndIsolation(
                runtime,
                image,
                context,
                descriptor
            );
            using (context.EnterSourceCode(image, sourceOwner))
            {
                object reservation = await NifAdmissionHostBridge.Reserve(runtime.Modules, context);
                try
                {
                    await NifAllocationDomainHostBridge.InitializeReservation(reservation);
                    Require(NifAllocationDomainHostBridge.Snapshot(runtime).JoinedOwners == 0);
                    passed.Add(NifAllocationDomainHostNames.InitializerCase);
                }
                finally
                {
                    await NifAdmissionHostBridge.Dispose(reservation);
                }
            }
            await ConcurrentSnapshotAndDispose(
                runtime,
                image,
                context,
                descriptor
            );
        }
        );
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifAllocationDomainHostNames.Broken);
    }

    private static void Coherent(NifAllocationDomainHostSnapshot value)
    {
        var blocks = value.Allocations;
        Require(value.JoinedOwners >= 0 && blocks.Count >= 0 && blocks.RequestedBytes <= blocks.BackingRequestBytes);
        Require(blocks.AllocatedRequestedBytes - blocks.ReleasedRequestedBytes == blocks.RequestedBytes);
        Require(blocks.AllocatedBackingRequestBytes - blocks.ReleasedBackingRequestBytes == blocks.BackingRequestBytes);
        Require(blocks.SuccessfulAllocations - blocks.ReleasedAllocations == blocks.Count);
        Require(blocks.PeakRequestedBytes >= blocks.RequestedBytes && blocks.PeakBackingRequestBytes >= blocks.BackingRequestBytes);
    }

    private static async Task ConstructedAndCancelled(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        ProcessContext context,
        ModuleCodeDescriptor descriptor
    )
    {
        var before = NifAllocationDomainHostBridge.Snapshot(runtime);
        await using var library = new NifAllocationDomainHostBridge(image, context);
        Require(NifAllocationDomainHostBridge.Snapshot(runtime) == before);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try
        {
            library.InitializeWrongModule(descriptor, cancelled.Token);
            throw new InvalidOperationException(NifAllocationDomainHostNames.UnexpectedInitialization);
        }
        catch (OperationCanceledException error) when (error.CancellationToken == cancelled.Token)
        {
            Require(NifAllocationDomainHostBridge.Snapshot(runtime) == before);
        }
        await library.DisposeAsync();
        Require(NifAllocationDomainHostBridge.Snapshot(runtime) == before);
        passed.Add(NifAllocationDomainHostNames.ConstructedCancelledCase);
    }

    private static async Task PayloadAndDisposal(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        ProcessContext context,
        ModuleCodeDescriptor descriptor
    )
    {
        await using var library = new NifAllocationDomainHostBridge(image, context);
        library.InitializeWrongModule(descriptor);
        nint pointer = library.Allocate(NifAllocationDomainHostParameters.FirstBlockBytes);
        Require(pointer != 0);
        Marshal.WriteByte(pointer, NifAllocationDomainHostParameters.FirstBlockBytes - 1, NifAllocationDomainHostParameters.PayloadMarker);
        Require(Marshal.ReadByte(pointer, NifAllocationDomainHostParameters.FirstBlockBytes - 1) == NifAllocationDomainHostParameters.PayloadMarker);
        Require(library.OwnerSnapshot.Count == 1);
        var one = NifAllocationDomainHostBridge.Snapshot(runtime);
        if (one.JoinedOwners != 1 || one.Allocations.Count != 1 || one.Allocations.RequestedBytes != NifAllocationDomainHostParameters.FirstBlockBytes)
            throw new InvalidOperationException(NifAllocationDomainHostNames.EnrollmentMissing);
        nint zero = library.Allocate(0);
        Require(zero != 0 && zero != pointer);
        Marshal.WriteByte(zero, NifAllocationDomainHostParameters.PayloadMarker);
        var both = NifAllocationDomainHostBridge.Snapshot(runtime);
        Require(both.Allocations.Count == NifAllocationDomainHostParameters.PayloadLiveBlocks && both.Allocations.RequestedBytes == one.Allocations.RequestedBytes && both.Allocations.BackingRequestBytes == one.Allocations.BackingRequestBytes + 1);
        Require(one.Allocations.Count == 1 && one.Allocations.BackingRequestBytes == NifAllocationDomainHostParameters.FirstBlockBytes);
        library.Free(pointer);
        await library.DisposeAsync();
        await library.DisposeAsync();
        var retained = NifAllocationDomainHostBridge.Snapshot(runtime);
        Require(retained.JoinedOwners == 1 && retained.Allocations.Count == 1 && library.OwnerSnapshot.Count == 1);
        Require(Marshal.ReadByte(zero) == NifAllocationDomainHostParameters.PayloadMarker);
        NativeResourceArena528Access.Free(zero);
        var closed = NifAllocationDomainHostBridge.Snapshot(runtime);
        Require(closed.JoinedOwners == 0 && closed.Allocations.Count == 0 && library.OwnerSnapshot.Count == 0);
        Require(closed.Allocations.AllocatedRequestedBytes == both.Allocations.AllocatedRequestedBytes && closed.Allocations.PeakBackingRequestBytes == both.Allocations.PeakBackingRequestBytes);
        Coherent(closed);
        passed.Add(NifAllocationDomainHostNames.PayloadCase);
    }

    private static async Task GlobalPeakAndIsolation(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        ProcessContext context,
        ModuleCodeDescriptor descriptor
    )
    {
        await using var otherRuntime = new ProcessRuntime();
        var isolated = NifAllocationDomainHostBridge.Snapshot(otherRuntime);
        await using var first = new NifAllocationDomainHostBridge(image, context);
        await using var second = new NifAllocationDomainHostBridge(image, context);
        first.InitializeWrongModule(descriptor);
        second.InitializeWrongModule(descriptor);
        nint firstPointer = first.Allocate(NifAllocationDomainHostParameters.FirstBlockBytes);
        nint secondPointer = second.Allocate(NifAllocationDomainHostParameters.SecondBlockBytes);
        Require(firstPointer != 0 && secondPointer != 0);
        var together = NifAllocationDomainHostBridge.Snapshot(runtime);
        Require(together.JoinedOwners == NifAllocationDomainHostParameters.SimultaneousOwners && together.Allocations.RequestedBytes == NifAllocationDomainHostParameters.FirstBlockBytes + NifAllocationDomainHostParameters.SecondBlockBytes);
        first.Free(firstPointer);
        nint later = second.Allocate(NifAllocationDomainHostParameters.LaterBlockBytes);
        Require(later != 0);
        var after = NifAllocationDomainHostBridge.Snapshot(runtime);
        Require(after.Allocations.RequestedBytes == NifAllocationDomainHostParameters.SecondBlockBytes + NifAllocationDomainHostParameters.LaterBlockBytes && after.Allocations.PeakRequestedBytes == together.Allocations.RequestedBytes);
        Require(NifAllocationDomainHostBridge.Snapshot(otherRuntime) == isolated);
        await first.DisposeAsync();
        Require(NifAllocationDomainHostBridge.Snapshot(runtime).JoinedOwners == 1);
        await second.DisposeAsync();
        Require(NifAllocationDomainHostBridge.Snapshot(runtime).Allocations.Count == 2);
        NativeResourceArena528Access.Free(secondPointer);
        NativeResourceArena528Access.Free(later);
        Require(NifAllocationDomainHostBridge.Snapshot(runtime).JoinedOwners == 0);
        Coherent(NifAllocationDomainHostBridge.Snapshot(runtime));
        passed.Add(NifAllocationDomainHostNames.AggregateCase);
    }

    private static async Task ConcurrentSnapshotAndDispose(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        ProcessContext context,
        ModuleCodeDescriptor descriptor
    )
    {
        await using var first = new NifAllocationDomainHostBridge(image, context);
        await using var second = new NifAllocationDomainHostBridge(image, context);
        first.InitializeWrongModule(descriptor);
        second.InitializeWrongModule(descriptor);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task held = Task.Run(async () =>
        {
            try
            {
                nint pointer = first.Allocate(NifAllocationDomainHostParameters.FirstBlockBytes);
                Require(pointer != 0);
                Marshal.WriteByte(pointer, NifAllocationDomainHostParameters.PayloadMarker);
                entered.SetResult();
                await release.Task.WaitAsync(NifAllocationDomainHostNames.Deadline);
                Require(Marshal.ReadByte(pointer) == NifAllocationDomainHostParameters.PayloadMarker);
                NativeResourceArena528Access.Free(pointer);
            }
            catch (Exception failure)
            {
                entered.TrySetException(failure);
                throw;
            }
        });
        try
        {
            await entered.Task.WaitAsync(NifAllocationDomainHostNames.Deadline);
            Require(NifAllocationDomainHostBridge.Snapshot(runtime).Allocations.Count == 1);
            Task writer = Task.Run(() =>
            {
                for (int index = 0; index < NifAllocationDomainHostParameters.ConcurrentIterations; index++)
                {
                    nint pointer = second.Allocate((nuint)(index % NifAllocationDomainHostParameters.ConcurrentBlockLimit));
                    Require(pointer != 0);
                    second.Free(pointer);
                }
            });
            using var deadline = new CancellationTokenSource(NifAllocationDomainHostNames.Deadline);
            do
            {
                deadline.Token.ThrowIfCancellationRequested();
                Coherent(NifAllocationDomainHostBridge.Snapshot(runtime));
                await Task.Yield();
            } while (!writer.IsCompleted);
            await writer.WaitAsync(NifAllocationDomainHostNames.Deadline);
            await first.DisposeAsync();
            var drained = NifAllocationDomainHostBridge.Snapshot(runtime);
            Require(drained.JoinedOwners == 2 && drained.Allocations.Count == 1 && first.OwnerSnapshot.Count == 1);
            release.TrySetResult();
            await held.WaitAsync(NifAllocationDomainHostNames.Deadline);
            Require(NifAllocationDomainHostBridge.Snapshot(runtime).Allocations.Count == 0 && first.OwnerSnapshot.Count == 0);
            await second.DisposeAsync();
            Require(NifAllocationDomainHostBridge.Snapshot(runtime).JoinedOwners == 0);
            Coherent(NifAllocationDomainHostBridge.Snapshot(runtime));
            passed.Add(NifAllocationDomainHostNames.ConcurrentCase);
        }
        finally
        {
            release.TrySetResult();
            await held.WaitAsync(NifAllocationDomainHostNames.Deadline);
        }
    }
}
