using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResourceAllocation570Names;

namespace Erlang.Tests;

internal static class NativeResourceAllocation570Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    // Real native backing and actual slab cells; not an OTP/public-metrics proof.
    internal static async Task SeparateNativeBytesAndGuestCells()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object heap = Property(context, Heap);
        object domain = Property(runtime, NativeResource524Names.Domain);
        var provider = Provider(runtime);
        object initialDomain = Call(domain, DomainSnapshot)!;
        long initialBytes = Number(initialDomain, NativeBytes);
        long initialCount = Number(initialDomain, NativeCount);
        long initialNativeReclaimed = Number(initialDomain, NativeReclaimed);
        object initialCells = Property(heap, Allocation);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        long actualNativeBytes = Number(payload, AllocationBytes);
        IDisposable? frame = null;
        bool nativeReleased = false;
        var failures = new List<Exception>();
        try
        {
            NativeResource524Access.Check(actualNativeBytes > NativeResource524Names.PayloadBytes);
            CheckDomain(domain, initialBytes + actualNativeBytes, initialCount + 1);
            NativeResource524Access.Check(Occupied(Property(heap, Allocation)) == Occupied(initialCells));
            BitString binary = Binary(runtime, payload);
            frame = (IDisposable)Call(heap, Enter)!;
            int binarySlot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                binarySlot,
                binary
            );
            // Native semantic headers currently have no registered slab cells.
            NativeResource524Access.Check(Occupied(Property(heap, Allocation)) == Occupied(initialCells));
            int tupleSlot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                tupleSlot,
                Term.Tuple(binary, binary)
            );
            NativeResource524Access.Check(Occupied(Property(heap, Allocation)) == Occupied(initialCells) + TupleAllocationWords);
            // Two views/aliases retain one actual backing allocation.
            CheckDomain(domain, initialBytes + actualNativeBytes, initialCount + 1);
            Call(payload, ReleaseNative);
            nativeReleased = true;
            NativeResource524Access.Check(destroyed == 0);
            frame.Dispose();
            frame = null;
            NativeResource524Access.Check(destroyed == 0);
            Call(heap, Collect);
            await ((Task)Property(payload, Retired)).WaitAsync(NativeResource524Names.Deadline);
            NativeResource524Access.Check(destroyed == 1);
            CheckDomain(domain, initialBytes, initialCount);
            NativeResource524Access.Check(Number(Call(domain, DomainSnapshot)!, NativeReclaimed) == initialNativeReclaimed + 1);
            object reclaimed = Property(heap, Allocation);
            NativeResource524Access.Check(Occupied(reclaimed) == Occupied(initialCells));
            NativeResource524Access.Check(Number(reclaimed, Reclaimed) == Number(initialCells, Reclaimed) + TupleAllocationWords);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                frame?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                if (!nativeReleased) Call(payload, ReleaseNative);
                Call(heap, Collect);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await ((Task)Property(payload, Retired)).WaitAsync(NativeResource524Names.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                await ((Task)Call(provider.Bindings, RetireImage)!).WaitAsync(NativeResource524Names.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            GC.KeepAlive(destructor);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static long Number(object value, string property) => Convert.ToInt64(Property(value, property));

    private static long Occupied(object snapshot)
        => checked(Number(snapshot, Nursery) + Number(snapshot, Survivor) + Number(snapshot, Old));

    private static void CheckDomain(object domain, long bytes, long count)
    {
        object snapshot = Call(domain, DomainSnapshot)!;
        NativeResource524Access.Check(Number(snapshot, NativeBytes) == bytes && Number(snapshot, NativeCount) == count);
    }
}
