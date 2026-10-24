using System.Runtime.InteropServices;

namespace Erlang.Tests;

public static class NifAllocationLedgerHostChecks
{
    private static readonly List<string> passed = new();
    internal static IReadOnlyList<string> Completed => passed.AsReadOnly();

    public static async Task Run()
    {
        passed.Clear();
        NifAllocationLedgerHostOwner.Preflight();
        ActualPayloadAndZero();
        ReclamationAndForeignFree();
        OwnerSeparation();
        OverflowBeforeNativeAllocation();
        await ConcurrentSnapshotAndReclamation();

    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifAllocationLedgerHostNames.Broken);
    }

    private static void Coherent(NifAllocationLedgerHostSnapshot value)
    {
        Require(value.Count >= 0 && value.RequestedBytes <= value.BackingRequestBytes);
        Require(value.PeakRequestedBytes >= value.RequestedBytes && value.PeakBackingRequestBytes >= value.BackingRequestBytes);
        Require(value.AllocatedRequestedBytes >= value.ReleasedRequestedBytes && value.AllocatedBackingRequestBytes >= value.ReleasedBackingRequestBytes);
        Require(value.SuccessfulAllocations >= value.ReleasedAllocations);
        Require(value.AllocatedRequestedBytes - value.ReleasedRequestedBytes == value.RequestedBytes);
        Require(value.AllocatedBackingRequestBytes - value.ReleasedBackingRequestBytes == value.BackingRequestBytes);
        Require(value.SuccessfulAllocations - value.ReleasedAllocations == value.Count);
    }

    private static void ActualPayloadAndZero()
    {
        var owner = new NifAllocationLedgerHostOwner();
        try
        {
            nint zero = owner.Allocate(0);
            Require(zero != 0);
            var zeroSnapshot = owner.Snapshot;
            Require(zeroSnapshot.Count == 1 && zeroSnapshot.RequestedBytes == 0 && zeroSnapshot.BackingRequestBytes == 1);
            Marshal.WriteByte(zero, NifAllocationLedgerHostParameters.PayloadMarker);
            Require(Marshal.ReadByte(zero) == NifAllocationLedgerHostParameters.PayloadMarker);
            nint block = owner.Allocate(NifAllocationLedgerHostParameters.NativeBlockBytes);
            Require(block != 0 && block != zero);
            Marshal.WriteByte(block, NifAllocationLedgerHostParameters.NativeBlockBytes - 1, NifAllocationLedgerHostParameters.PayloadMarker);
            Require(Marshal.ReadByte(block, NifAllocationLedgerHostParameters.NativeBlockBytes - 1) == NifAllocationLedgerHostParameters.PayloadMarker);
            var both = owner.Snapshot;
            Require(both.Count == NifAllocationLedgerHostParameters.PayloadScenarioLiveBlocks && both.RequestedBytes == NifAllocationLedgerHostParameters.NativeBlockBytes && both.BackingRequestBytes == NifAllocationLedgerHostParameters.NativeBlockBytes + 1);
            Require(zeroSnapshot.Count == 1 && zeroSnapshot.BackingRequestBytes == 1);
            owner.Free(zero);
            owner.Free(block);
            owner.Free(0);
            var empty = owner.Snapshot;
            Require(empty.Count == 0 && empty.RequestedBytes == 0 && empty.BackingRequestBytes == 0 && empty.SuccessfulAllocations == NifAllocationLedgerHostParameters.PayloadScenarioAllocations && empty.ReleasedAllocations == NifAllocationLedgerHostParameters.PayloadScenarioAllocations);
            Require(empty.PeakRequestedBytes == both.RequestedBytes && empty.PeakBackingRequestBytes == both.BackingRequestBytes);
            Coherent(empty);
            passed.Add(NifAllocationLedgerHostNames.RealPayloadCase);
        }
        finally
        {
            owner.ReclaimAll();
        }
    }

    private static void Foreign(NifAllocationLedgerHostOwner owner, nint pointer)
    {
        var before = owner.Snapshot;
        try
        {
            owner.Free(pointer);
            throw new InvalidOperationException(NifAllocationLedgerHostNames.ForeignAccepted);
        }
        catch (InvalidOperationException error) when (error.Message == NifAllocationLedgerHostNames.ForeignAllocationFree)
        {
            Require(owner.Snapshot == before);
        }
    }

    private static void ReclamationAndForeignFree()
    {
        var owner = new NifAllocationLedgerHostOwner();
        nint first = owner.Allocate(NifAllocationLedgerHostParameters.NativeBlockBytes);
        owner.Free(first);
        Foreign(owner, first);
        nint second = owner.Allocate(0);
        owner.ReclaimAll();
        var reclaimed = owner.Snapshot;
        Foreign(owner, second);
        owner.ReclaimAll();
        Require(owner.Snapshot == reclaimed && reclaimed.Count == 0 && reclaimed.ReleasedAllocations == NifAllocationLedgerHostParameters.ReclamationInitialAllocations);
        // Existing owner semantics permit reuse after ReclaimAll; it is not a closed/disposed object.
        nint third = owner.Allocate(1);
        owner.Free(third);
        Coherent(owner.Snapshot);
        passed.Add(NifAllocationLedgerHostNames.ReclamationCase);
    }

    private static void OwnerSeparation()
    {
        var first = new NifAllocationLedgerHostOwner();
        var second = new NifAllocationLedgerHostOwner();
        try
        {
            nint pointer = first.Allocate(1);
            Foreign(second, pointer);
            Require(first.Snapshot.Count == 1 && second.Snapshot == default);
        }
        finally
        {
            first.ReclaimAll();
            second.ReclaimAll();
        }
        passed.Add(NifAllocationLedgerHostNames.OwnerSeparationCase);
    }

    private static void OverflowBeforeNativeAllocation()
    {
        var owner = new NifAllocationLedgerHostOwner();
        var cases = new[]
        {
            default(NifAllocationLedgerHostSnapshot) with { Count = int.MaxValue },
            default(NifAllocationLedgerHostSnapshot) with { RequestedBytes = ulong.MaxValue },
            default(NifAllocationLedgerHostSnapshot) with { BackingRequestBytes = ulong.MaxValue },
            default(NifAllocationLedgerHostSnapshot) with { AllocatedRequestedBytes = ulong.MaxValue },
            default(NifAllocationLedgerHostSnapshot) with { AllocatedBackingRequestBytes = ulong.MaxValue },
            default(NifAllocationLedgerHostSnapshot) with { SuccessfulAllocations = long.MaxValue }
        };
        foreach (var saturated in cases)
        {
            owner.SetSnapshotForOverflow(saturated);
            try
            {
                owner.Allocate(1);
                throw new InvalidOperationException(NifAllocationLedgerHostNames.OverflowAccepted);
            }
            catch (OverflowException)
            {
                Require(owner.Count == 0 && owner.Snapshot == saturated);
            }
        }
        owner.SetSnapshotForOverflow(default);
        Require(owner.Allocate(1) != 0);
        owner.ReclaimAll();
        Coherent(owner.Snapshot);
        passed.Add(NifAllocationLedgerHostNames.OverflowCase);
    }

    private static async Task ConcurrentSnapshotAndReclamation()
    {
        var owner = new NifAllocationLedgerHostOwner();
        // Mirrors the actual NifNativeLibraryOwner gate: active native frames exclude reclamation.
        var lifecycle = new object();
        long allocated = 0;
        long samples = 0;
        long liveSamples = 0;
        var observedLive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reading = new CancellationTokenSource();
        var reader = Task.Run(async () =>
        {
            while (!reading.IsCancellationRequested)
            {
                var current = owner.Snapshot;
                Coherent(current);
                if (current.Count > 0)
                {
                    Interlocked.Increment(ref liveSamples);
                    observedLive.TrySetResult();
                }
                Interlocked.Increment(ref samples);
                await Task.Yield();
            }
        });
        try
        {
            var workers = Enumerable.Range(0, NifAllocationLedgerHostParameters.ConcurrentWorkers).Select(worker => Task.Run(() =>
            {
                for (int iteration = 0; iteration < NifAllocationLedgerHostParameters.AllocationsPerWorker; iteration++)
                {
                    lock (lifecycle)
                    {
                        nint pointer = owner.Allocate((nuint)(iteration % NifAllocationLedgerHostParameters.NativeBlockBytes));
                        Require(pointer != 0);
                        if (worker == 0 && iteration == 0)
                            observedLive.Task.WaitAsync(NifAllocationLedgerHostParameters.Deadline).GetAwaiter().GetResult();
                        Marshal.WriteByte(pointer, NifAllocationLedgerHostParameters.PayloadMarker);
                        Interlocked.Increment(ref allocated);
                        if (iteration % NifAllocationLedgerHostParameters.RetainEvery != 0)
                            owner.Free(pointer);
                    }
                }
            })).ToArray();
            var reclaim = Task.Run(() =>
            {
                for (int round = 0; round < NifAllocationLedgerHostParameters.ReclamationRounds; round++)
                {
                    lock (lifecycle)
                        owner.ReclaimAll();
                    Thread.Yield();
                }
            });
            await Task.WhenAll(workers.Append(reclaim)).WaitAsync(NifAllocationLedgerHostParameters.Deadline);
            lock (lifecycle)
                owner.ReclaimAll();
            var final = owner.Snapshot;
            Coherent(final);
            Require(final.Count == 0 && final.SuccessfulAllocations == allocated && final.ReleasedAllocations == allocated && allocated == NifAllocationLedgerHostParameters.ConcurrentWorkers * NifAllocationLedgerHostParameters.AllocationsPerWorker);
            ulong expectedRequested = (ulong)(NifAllocationLedgerHostParameters.ConcurrentWorkers * Enumerable.Range(0, NifAllocationLedgerHostParameters.AllocationsPerWorker).Sum(iteration => iteration % NifAllocationLedgerHostParameters.NativeBlockBytes));
            ulong zeroBlocks = (ulong)(NifAllocationLedgerHostParameters.ConcurrentWorkers * ((NifAllocationLedgerHostParameters.AllocationsPerWorker - 1) / NifAllocationLedgerHostParameters.NativeBlockBytes + 1));
            Require(final.AllocatedRequestedBytes == expectedRequested && final.AllocatedBackingRequestBytes == expectedRequested + zeroBlocks);
        }
        finally
        {
            reading.Cancel();
            await reader.WaitAsync(NifAllocationLedgerHostParameters.Deadline);
            lock (lifecycle)
                owner.ReclaimAll();
        }
        Require(samples > 0 && liveSamples > 0);
        passed.Add(NifAllocationLedgerHostNames.ConcurrentSnapshotCase);
    }
}
