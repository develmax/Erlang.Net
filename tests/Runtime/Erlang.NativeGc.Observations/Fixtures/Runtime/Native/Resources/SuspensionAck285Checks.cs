using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// Real transition tests. No queue flags, synthetic native payload, or runtime
// admission bypass is installed. The exact pinned BCL normal-return proof
// is separate from these exercised partial-admission and drain transitions.
internal static class SuspensionAck285Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run(bool owned)
    {
        ThreadPoolReturnProof299Checks.Run();

        await One(owned, partialAdmission: true);
        await One(owned, partialAdmission: false);
    }

    private static async Task One(bool owned, bool partialAdmission)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object first = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        object? second = null;
        object? tag = null;
        Task? image = null;
        bool firstReleased = false, secondReleased = false;
        try
        {
            second = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
            Marshal.WriteByte((nint)Property(first, SuspensionRootLease821Names.Pointer), 0, SuspensionAck285Names.First);
            Marshal.WriteByte((nint)Property(second, SuspensionRootLease821Names.Pointer), 0, SuspensionAck285Names.Second);
            BitString firstBinary = Binary(runtime, first), secondBinary = Binary(runtime, second);
            Term value = Term.Tuple(firstBinary, secondBinary);
            Type portableType = typeof(ProcessRuntime).Assembly.GetType(SuspensionAck285Names.Portable, true)!;
            object portable = portableType.GetMethod(SuspensionAck285Names.Capture,
                BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [value])!;
            object owner = Property(runtime, SuspensionAck285Names.Roots);
            tag = Call(
                owner,
                SuspensionAck285Names.Prepare,
                runtime,
                portable
            )!;
            Array reservations = (Array)Field(tag, SuspensionAck285Names.Reservations);
            Check(reservations.Length == 2 && References(first) == 1 && References(second) == 1);
            object graph = Field(tag, SuspensionAck285Names.Graph);
            object work = Property(graph, SuspensionAck285Names.Work);
            Task completion = (Task)Property(Property(work, SuspensionAck285Names.Completion), SuspensionAck285Names.Task);

            if (partialAdmission)
            {
                // Dispose is the actual supported reservation cancellation
                // transition; Admit172 must fail AFTER admitting its first leaf.
                ((IDisposable)reservations.GetValue(1)!).Dispose();
                bool rejected = false;
                lock (Field(runtime, SuspensionAck285Names.Gate))
                    try
                    {
                        Call(tag, SuspensionAck285Names.Admit);
                    }
                    catch (InvalidOperationException)
                    {
                        rejected = true;
                    }
                object firstLease = reservations.GetValue(0)!;
                bool firstOwnsFirst = (bool)Call(firstLease, SuspensionAck285Names.BelongsTo, first)!;
                Check(rejected && References(firstOwnsFirst ? first : second) == 2
                    && References(firstOwnsFirst ? second : first) == 1 && destroyed == 0);
                Check(firstBinary.ToArray()[0] == SuspensionAck285Names.First
                    && secondBinary.ToArray()[0] == SuspensionAck285Names.Second);
            }
            else
            {
                lock (Field(runtime, SuspensionAck285Names.Gate)) Call(tag, SuspensionAck285Names.Admit);
                Check(References(first) == 2 && References(second) == 2);
                // Close the REAL cleanup owner and await its actual ACK pump.
                // The still-owned graph prevents work completion at this point.
                Call(owner, SuspensionAck285Names.Stop);
                Task pump = (Task)Field(owner, SuspensionAck285Names.Pump);
                await pump.WaitAsync(Deadline);
                Check(!completion.IsCompleted);
                Task drain = (Task)Call(owner, SuspensionAck285Names.Drain)!;
                Check(!drain.IsCompleted);
                Call(first, SuspensionRootLease821Names.ReleaseNative);
                firstReleased = true;
                Call(second, SuspensionRootLease821Names.ReleaseNative);
                secondReleased = true;
                image = (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
                Check(destroyed == 0 && !image.IsCompleted);
                Check(firstBinary.ToArray()[0] == SuspensionAck285Names.First
                    && secondBinary.ToArray()[0] == SuspensionAck285Names.Second);
                await ((IAsyncDisposable)tag).DisposeAsync();
                await drain.WaitAsync(Deadline);
            }
            if (!firstReleased)
            {
                Call(first, SuspensionRootLease821Names.ReleaseNative);
                firstReleased = true;
            }
            if (!secondReleased)
            {
                Call(second, SuspensionRootLease821Names.ReleaseNative);
                secondReleased = true;
            }
            image ??= (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            await ((IAsyncDisposable)tag).DisposeAsync();
            await completion.WaitAsync(Deadline);
            await Task.WhenAll(
                (Task)Property(first, SuspensionRootLease821Names.Retired),
                (Task)Property(second, SuspensionRootLease821Names.Retired),
                image
            ).WaitAsync(Deadline);
            Check(destroyed == 2 && provider.Owner.Disposed && Property(work, SuspensionAck285Names.Failure) is null);
            GC.KeepAlive(value);
        }
        finally
        {
            if (tag is not null) await ((IAsyncDisposable)tag).DisposeAsync();
            if (!firstReleased) Call(first, SuspensionRootLease821Names.ReleaseNative);
            if (second is not null && !secondReleased) Call(second, SuspensionRootLease821Names.ReleaseNative);
            await runtime.DisposeAsync();
            await ((Task)Property(first, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            if (second is not null) await ((Task)Property(second, SuspensionRootLease821Names.Retired)).WaitAsync(Deadline);
            image ??= (Task)Call(provider.Bindings, SuspensionRootLease821Names.RetireImage)!;
            await image.WaitAsync(Deadline);
            GC.KeepAlive(destructor);
        }
    }

    private static object Field(object value, string name)
        => value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static long References(object payload)
    {
        Type header = typeof(ProcessRuntime).Assembly.GetType(SuspensionRootLease821Names.Header, true)!;
        nint allocation = (nint)Field(payload, SuspensionRootLease821Names.Allocation);
        int offset = checked((int)Marshal.OffsetOf(header, SuspensionRootLease821Names.RefCount));

        return Marshal.ReadInt64(allocation, offset);
    }
}
