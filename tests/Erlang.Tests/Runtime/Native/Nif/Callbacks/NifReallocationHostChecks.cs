using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Erlang.Tests;

public static class NifReallocationHostChecks
{
    public static async Task Run()
    {
        NifReallocationHostOwner.Preflight();
        NullAndZero();
        GrowShrinkAndHistories();
        ForeignAndReclaimed();
        OwnerOverflow();
        await JoinedDomain();
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifReallocationHostNames.Broken);
    }

    private static void Coherent(NifAllocationLedgerHostSnapshot value)
    {
        Require(value.Count >= 0 && value.RequestedBytes <= value.BackingRequestBytes);
        Require(value.AllocatedRequestedBytes >= value.ReleasedRequestedBytes && value.AllocatedBackingRequestBytes >= value.ReleasedBackingRequestBytes);
        Require(value.AllocatedRequestedBytes - value.ReleasedRequestedBytes == value.RequestedBytes);
        Require(value.AllocatedBackingRequestBytes - value.ReleasedBackingRequestBytes == value.BackingRequestBytes);
        Require(value.SuccessfulAllocations - value.ReleasedAllocations == value.Count);
        Require(value.AllocatedRequestedBytes <= value.AllocatedBackingRequestBytes && value.ReleasedRequestedBytes <= value.ReleasedBackingRequestBytes);
        Require(value.PeakRequestedBytes >= value.RequestedBytes && value.PeakBackingRequestBytes >= value.BackingRequestBytes);
    }

    private static void Write(nint pointer, int size)
    {
        Require(pointer != 0);
        for (int index = 0; index < size; index++)
            Marshal.WriteByte(pointer, index, (byte)(NifReallocationHostParameters.Marker + index));
    }

    private static void Read(nint pointer, int size)
    {
        for (int index = 0; index < size; index++)
            Require(Marshal.ReadByte(pointer, index) == (byte)(NifReallocationHostParameters.Marker + index));
    }

    private static void NullAndZero()
    {
        var owner = new NifReallocationHostOwner();
        try
        {
            nint zero = owner.Reallocate(0, 0);
            Require(zero != 0 && owner.Snapshot.Count == 1 && owner.Snapshot.RequestedBytes == 0 && owner.Snapshot.BackingRequestBytes == 1);
            zero = owner.Reallocate(zero, 0);
            Require(zero != 0 && owner.Snapshot.Count == 1 && owner.Snapshot.SuccessfulAllocations == 1 && owner.Snapshot.ReleasedAllocations == 0);
            owner.Free(zero);
            nint positive = owner.Reallocate(0, NifReallocationHostParameters.Initial);
            Write(positive, NifReallocationHostParameters.Initial);
            Read(positive, NifReallocationHostParameters.Initial);
            owner.Free(positive);
            Coherent(owner.Snapshot);
        }
        finally
        {
            owner.Reclaim();
        }
    }

    private static void Resize(
        NifReallocationHostOwner owner,
        ref nint pointer,
        int oldSize,
        int newSize
    )
    {
        var before = owner.Snapshot;
        pointer = owner.Reallocate(pointer, (nuint)newSize);
        Require(pointer != 0);
        Read(pointer, Math.Min(oldSize, newSize));
        Write(pointer, newSize);
        var after = owner.Snapshot;
        Require(after.Count == before.Count && after.SuccessfulAllocations == before.SuccessfulAllocations && after.ReleasedAllocations == before.ReleasedAllocations);
        Require(after.AllocatedRequestedBytes == before.AllocatedRequestedBytes + (ulong)newSize && after.ReleasedRequestedBytes == before.ReleasedRequestedBytes + (ulong)oldSize);
        Require(after.AllocatedBackingRequestBytes == before.AllocatedBackingRequestBytes + (ulong)Math.Max(newSize, 1) && after.ReleasedBackingRequestBytes == before.ReleasedBackingRequestBytes + (ulong)Math.Max(oldSize, 1));
        Coherent(after);
    }

    private static void GrowShrinkAndHistories()
    {
        var owner = new NifReallocationHostOwner();
        try
        {
            nint pointer = owner.Allocate(NifReallocationHostParameters.Initial);
            Write(pointer, NifReallocationHostParameters.Initial);
            var immutable = owner.Snapshot;
            Resize(
                owner,
                ref pointer,
                NifReallocationHostParameters.Initial,
                NifReallocationHostParameters.Grown
            );
            Resize(
                owner,
                ref pointer,
                NifReallocationHostParameters.Grown,
                NifReallocationHostParameters.Shrunk
            );
            Resize(
                owner,
                ref pointer,
                NifReallocationHostParameters.Shrunk,
                NifReallocationHostParameters.Shrunk
            );
            Resize(
                owner,
                ref pointer,
                NifReallocationHostParameters.Shrunk,
                0
            );
            Require(owner.Snapshot.Count == 1 && owner.Snapshot.BackingRequestBytes == 1);
            Resize(
                owner,
                ref pointer,
                0,
                NifReallocationHostParameters.AfterZero
            );
            Require(immutable.RequestedBytes == NifReallocationHostParameters.Initial && immutable.PeakRequestedBytes == NifReallocationHostParameters.Initial);
            owner.Free(pointer);
            Require(owner.Snapshot.Count == 0 && owner.Snapshot.SuccessfulAllocations == 1 && owner.Snapshot.ReleasedAllocations == 1);
            Coherent(owner.Snapshot);
        }
        finally
        {
            owner.Reclaim();
        }
    }

    private static void Refused(NifReallocationHostOwner owner, nint pointer)
    {
        var before = owner.Snapshot;
        try
        {
            owner.Reallocate(pointer, NifReallocationHostParameters.Shrunk);
            throw new InvalidOperationException(NifReallocationHostNames.Broken);
        }
        catch (InvalidOperationException failure) when (failure.Message == NifReallocationHostNames.Foreign)
        {
            Require(owner.Snapshot == before);
        }
    }

    private static void ForeignAndReclaimed()
    {
        var first = new NifReallocationHostOwner();
        var second = new NifReallocationHostOwner();
        try
        {
            nint a = first.Allocate(NifReallocationHostParameters.Initial);
            nint b = second.Allocate(NifReallocationHostParameters.Other);
            Write(a, NifReallocationHostParameters.Initial);
            Write(b, NifReallocationHostParameters.Other);
            Refused(first, b);
            Refused(second, a);
            Read(a, NifReallocationHostParameters.Initial);
            Read(b, NifReallocationHostParameters.Other);
            first.Reclaim();
            Refused(first, a);
        }
        finally
        {
            first.Reclaim();
            second.Reclaim();
        }
    }

    private static NifAllocationLedgerHostSnapshot OverflowSeed(NifAllocationLedgerHostSnapshot value) => value with
    {
        AllocatedRequestedBytes = ulong.MaxValue,
        ReleasedRequestedBytes = ulong.MaxValue - value.RequestedBytes,
        AllocatedBackingRequestBytes = ulong.MaxValue,
        ReleasedBackingRequestBytes = ulong.MaxValue - value.BackingRequestBytes
    };

    private static void ExpectOverflow(Action operation)
    {
        try
        {
            operation();
        }
        catch (OverflowException)
        {
            return;
        }
        throw new InvalidOperationException(NifReallocationHostNames.OverflowAccepted);
    }

    private static void OwnerOverflow()
    {
        var owner = new NifReallocationHostOwner();
        nint pointer = owner.Allocate(NifReallocationHostParameters.Initial);
        Write(pointer, NifReallocationHostParameters.Initial);
        var before = owner.Snapshot;
        var synthetic = OverflowSeed(before);
        try
        {
            owner.SeedSyntheticSnapshot(synthetic);
            ExpectOverflow(() => owner.Reallocate(pointer, NifReallocationHostParameters.Shrunk));
            Require(owner.Snapshot == synthetic);
            Read(pointer, NifReallocationHostParameters.Initial);
        }
        finally
        {
            owner.SeedSyntheticSnapshot(before);
            owner.Reclaim();
        }
    }

    private static async Task JoinedDomain()
    {
        await using var runtime = new ProcessRuntime();
        var source = new object();
        var descriptor = new ModuleCodeDescriptor(
            [(NifReallocationHostNames.Function, 0)],
            [(NifReallocationHostNames.Function, 0)],
            new BitString(new byte[MD5.HashSizeInBytes]),
            Nil.Value,
            sourceOwner: source
        );
        runtime.Modules.RegisterSource(
            NifReallocationHostNames.Module,
            NifReallocationHostNames.Function,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value),
            source
        );
        runtime.Modules.PublishModuleCode(NifReallocationHostNames.Module, descriptor);
        var image = runtime.Modules.CurrentCodeImage(NifReallocationHostNames.Module)!;
        await NifAdmissionHostBridge.Context(
            runtime,
            async context =>
        {
            await using var libraryA = new NifAllocationDomainHostBridge(image, context);
            await using var libraryB = new NifAllocationDomainHostBridge(image, context);
            libraryA.InitializeWrongModule(descriptor);
            libraryB.InitializeWrongModule(descriptor);
            var a = NifReallocationHostOwner.Library(libraryA);
            var b = NifReallocationHostOwner.Library(libraryB);
            nint pointer = a.Allocate(NifReallocationHostParameters.Initial);
            Write(pointer, NifReallocationHostParameters.Initial);
            object domain = NifReallocationHostOwner.RuntimeDomain(runtime);
            var baseline = NifReallocationHostOwner.DomainSnapshot(domain);
            var synthetic = OverflowSeed(baseline);
            try
            {
                NifReallocationHostOwner.Seed(domain, synthetic);
                ExpectOverflow(() => a.Reallocate(pointer, NifReallocationHostParameters.Shrunk));
                Require(NifReallocationHostOwner.DomainSnapshot(domain) == synthetic && a.Snapshot == baseline);
                Read(pointer, NifReallocationHostParameters.Initial);
            }
            finally
            {
                NifReallocationHostOwner.Seed(domain, baseline);
            }
            nint other = b.Allocate(NifReallocationHostParameters.Other);
            Write(other, NifReallocationHostParameters.Other);
            Resize(
                a,
                ref pointer,
                NifReallocationHostParameters.Initial,
                NifReallocationHostParameters.Grown
            );
            Resize(
                a,
                ref pointer,
                NifReallocationHostParameters.Grown,
                NifReallocationHostParameters.Shrunk
            );
            Resize(
                b,
                ref other,
                NifReallocationHostParameters.Other,
                NifReallocationHostParameters.OtherGrown
            );
            Require(NifAllocationDomainHostBridge.Snapshot(runtime).Allocations.PeakRequestedBytes == NifReallocationHostParameters.GlobalPeak);
            Resize(
                a,
                ref pointer,
                NifReallocationHostParameters.Shrunk,
                0
            );
            Require(a.Snapshot.Count == 1 && NifAllocationDomainHostBridge.Snapshot(runtime).JoinedOwners == NifAllocationDomainHostParameters.SimultaneousOwners);
            Resize(
                a,
                ref pointer,
                0,
                NifReallocationHostParameters.AfterZero
            );
            pointer = await ConcurrentSnapshots(runtime, a, pointer);
            await libraryA.DisposeAsync();
            await libraryB.DisposeAsync();
            var retained = NifAllocationDomainHostBridge.Snapshot(runtime);
            Require(retained.JoinedOwners == NifAllocationDomainHostParameters.SimultaneousOwners && retained.Allocations.Count == 2);
            Read(pointer, NifReallocationHostParameters.AfterZero);
            Read(other, NifReallocationHostParameters.OtherGrown);
            NativeResourceArena528Access.Free(pointer);
            NativeResourceArena528Access.Free(other);
            var closed = NifAllocationDomainHostBridge.Snapshot(runtime);
            Require(closed.JoinedOwners == 0 && closed.Allocations.Count == 0);
            Coherent(closed.Allocations);
        }
        );
    }

    private static async Task<nint> ConcurrentSnapshots(ProcessRuntime runtime, NifReallocationHostOwner owner, nint pointer)
    {
        using var writerStop = new CancellationTokenSource(NifAllocationDomainHostNames.Deadline);
        Task<nint> writer = Task.Run(() =>
        {
            int size = NifReallocationHostParameters.AfterZero;
            for (int index = 0; index < NifReallocationHostParameters.Iterations; index++)
            {
                writerStop.Token.ThrowIfCancellationRequested();
                int next = size == NifReallocationHostParameters.AfterZero ? NifReallocationHostParameters.Shrunk : NifReallocationHostParameters.AfterZero;
                Resize(
                    owner,
                    ref pointer,
                    size,
                    next
                );
                size = next;
            }

            return pointer;
        });
        Exception? readerFailure = null;
        try
        {
            using var deadline = new CancellationTokenSource(NifAllocationDomainHostNames.Deadline);
            do
            {
                deadline.Token.ThrowIfCancellationRequested();
                Coherent(NifAllocationDomainHostBridge.Snapshot(runtime).Allocations);
                await Task.Yield();
            } while (!writer.IsCompleted);
            await writer.WaitAsync(NifAllocationDomainHostNames.Deadline);
        }
        catch (Exception failure)
        {
            readerFailure = failure;
            throw;
        }
        finally
        {
            writerStop.Cancel();
            try
            {
                // Join actual settlement before outer library disposal can free the pointer.
                // A WaitAsync timeout here would permit unsafe disposal while a writer still runs.
                await writer;
            }
            catch (Exception) when (readerFailure is not null)
            {
                // The writer has settled; preserve the original reader/observed writer failure.
            }
        }

        // Return the settled writer's actual final allocation; realloc may have moved it.
        return await writer;
    }
}
