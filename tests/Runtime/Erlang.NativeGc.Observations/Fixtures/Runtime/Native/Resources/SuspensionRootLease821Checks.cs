using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// Actual ACK lifecycle integration plus the original native leaf race controls.
internal static class SuspensionRootLease821Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task ReserveCommitCancel()
    {
        foreach (bool enabled in new[] { false, true })
        {
            await SuspensionAck172Checks.Run(enabled);
            await SuspensionAck175Checks.Run(enabled);
            await SuspensionAck285Checks.Run(enabled);
            await One(enabled);
            await ForeignPayload(enabled);
            await CommitDisposeRace(enabled);
        }
    }

    private static async Task One(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        var provider = Provider(runtime);
        object domain = Property(runtime, Domain);
        object baseline = Call(domain, SuspensionRootLease821Names.Snapshot)!;
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        nint pointer = (nint)Property(payload, SuspensionRootLease821Names.Pointer);
        Marshal.WriteByte(pointer, 0, SuspensionRootLease821Names.Sentinel);
        BitString binary = Binary(runtime, payload);
        object first = Call(payload, SuspensionRootLease821Names.Reserve)!;
        object cancelled = Call(payload, SuspensionRootLease821Names.Reserve)!;
        object staleReservation = Call(payload, SuspensionRootLease821Names.Reserve)!;
        Task? imageRetirement = null;
        bool nativeReleased = false;
        try
        {
            using ((IDisposable)Call(payload, SuspensionRootLease821Names.NormalRetain)!)
                Check(PhysicalReferences(payload) == 2 && destroyed == 0);
            Check(PhysicalReferences(payload) == 1 && destroyed == 0);
            ((IDisposable)cancelled).Dispose();
            Check(PhysicalReferences(payload) == 1);
            bool rejected = false;
            try
            {
                Call(payload, SuspensionRootLease821Names.Admit, cancelled);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected && PhysicalReferences(payload) == 1);
            Call(payload, SuspensionRootLease821Names.Admit, first);
            Check(PhysicalReferences(payload) == 2 && binary.ToArray()[0] == SuspensionRootLease821Names.Sentinel);
            bool repeated = false;
            try
            {
                Call(payload, SuspensionRootLease821Names.Admit, first);
            }
            catch (InvalidOperationException)
            {
                repeated = true;
            }
            Check(repeated && PhysicalReferences(payload) == 2);
            Call(payload, SuspensionRootLease821Names.ReleaseNative);
            nativeReleased = true;
            Check(PhysicalReferences(payload) == 1 && destroyed == 0);
            imageRetirement = (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            Check(!imageRetirement.IsCompleted && !provider.Owner.Disposed);
            Check(binary.ToArray()[0] == SuspensionRootLease821Names.Sentinel);
            Call(first, SuspensionRootLease821Names.DisposeGuest);
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            await imageRetirement.WaitAsync(Deadline);
            Check(destroyed == 1 && provider.Owner.Disposed);
            object final = Call(domain, SuspensionRootLease821Names.Snapshot)!;
            Check((long)Property(final, SuspensionRootLease821Names.Created) == (long)Property(baseline, SuspensionRootLease821Names.Created) + 1);
            Check((long)Property(final, SuspensionRootLease821Names.Reclaimed) == (long)Property(baseline, SuspensionRootLease821Names.Reclaimed) + 1);
            Check((long)Property(final, SuspensionRootLease821Names.NativeBytes) == (long)Property(baseline, SuspensionRootLease821Names.NativeBytes));
            Check((int)Property(final, SuspensionRootLease821Names.Live) == (int)Property(baseline, SuspensionRootLease821Names.Live));
            Check((int)Property(final, SuspensionRootLease821Names.Pending) == 0 && Property(final, SuspensionRootLease821Names.Failure) is null);
            bool staleAdmission = false;
            try
            {
                Call(payload, SuspensionRootLease821Names.Admit, staleReservation);
            }
            catch (ObjectDisposedException)
            {
                staleAdmission = true;
            }
            Check(staleAdmission);
            ((IDisposable)staleReservation).Dispose();
            bool stale = false;
            try
            {
                _ = binary.ToArray();
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Check(stale);
        }
        finally
        {
            ((IDisposable)cancelled).Dispose();
            ((IDisposable)staleReservation).Dispose();
            ((IDisposable)first).Dispose();
            if (!nativeReleased) Call(payload, SuspensionRootLease821Names.ReleaseNative);
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            imageRetirement ??= (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            await imageRetirement.WaitAsync(Deadline);
            GC.KeepAlive(binary);
            GC.KeepAlive(destructor);
        }
    }

    private static async Task ForeignPayload(bool enabled)
    {
        await using var firstRuntime = OwnedGuest506RuntimeFactory.Create(enabled);
        await using var secondRuntime = OwnedGuest506RuntimeFactory.Create(enabled);
        var firstProvider = Provider(firstRuntime);
        var secondProvider = Provider(secondRuntime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object firstPayload = Allocate(firstRuntime, firstProvider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        object secondPayload = Allocate(secondRuntime, secondProvider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        object lease = Call(firstPayload, SuspensionRootLease821Names.Reserve)!;
        try
        {
            bool rejected = false;
            try
            {
                Call(secondPayload, SuspensionRootLease821Names.Admit, lease);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected && PhysicalReferences(firstPayload) == 1 && PhysicalReferences(secondPayload) == 1 && destroyed == 0);
        }
        finally
        {
            ((IDisposable)lease).Dispose();
            Call(firstPayload, SuspensionRootLease821Names.ReleaseNative);
            Call(secondPayload, SuspensionRootLease821Names.ReleaseNative);
            await Task.WhenAll(
                (Task)Property(firstPayload, SuspensionRootLease821Names.Retired),
                (Task)Property(secondPayload, SuspensionRootLease821Names.Retired)
            ).WaitAsync(Deadline);
            await Task.WhenAll(
                (Task)Call(firstProvider.Bindings, SuspensionRootLease821Names.RetireImage)!,
                (Task)Call(secondProvider.Bindings, SuspensionRootLease821Names.RetireImage)!
            ).WaitAsync(Deadline);
            GC.KeepAlive(destructor);
        }
        Check(destroyed == 2 && firstProvider.Owner.Disposed && secondProvider.Owner.Disposed);
    }

    private static async Task CommitDisposeRace(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        try
        {
            for (int index = 0; index < SuspensionRootLease821Names.RaceAttempts; index++)
            {
                object lease = Call(payload, SuspensionRootLease821Names.Reserve)!;
                var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Task commit = Task.Run(async () =>
                {
                    await start.Task;
                    try
                    {
                        Call(payload, SuspensionRootLease821Names.Admit, lease);
                    }
                    catch (InvalidOperationException) { }
                });
                Task dispose = Task.Run(async () =>
                {
                    await start.Task;
                    ((IDisposable)lease).Dispose();
                });
                start.SetResult();
                try
                {
                    await Task.WhenAll(commit, dispose).WaitAsync(Deadline);
                }
                finally
                {
                    ((IDisposable)lease).Dispose();
                }
                Check(PhysicalReferences(payload) == 1 && destroyed == 0);
            }
        }
        finally
        {
            Call(payload, SuspensionRootLease821Names.ReleaseNative);
            await ((Task)Property(payload, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            await ((Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!).WaitAsync(Deadline);
            GC.KeepAlive(destructor);
        }
        Check(destroyed == 1 && provider.Owner.Disposed);
    }

    private static long PhysicalReferences(object payload)
    {
        Type header = typeof(ProcessRuntime).Assembly.GetType(SuspensionRootLease821Names.Header, true)!;
        nint allocation = (nint)payload.GetType().GetField(SuspensionRootLease821Names.Allocation, Members)!.GetValue(payload)!;
        int offset = checked((int)Marshal.OffsetOf(header, SuspensionRootLease821Names.RefCount));

        return Marshal.ReadInt64(allocation, offset);
    }
}
