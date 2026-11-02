using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal sealed unsafe partial class NifNativeAllocationOwner
{
    private static readonly ConcurrentDictionary<nint, NifNativeMutex795> mutexAddresses795 = new();
    private readonly Dictionary<nint, NifNativeMutex795> mutexes795 = new();
    private bool mutexesClosed795;

    internal static int LiveMutexCount795 => mutexAddresses795.Count;

    internal nint CreateMutex795(byte* input)
    {
        byte[] bytes;
        if (input == null) bytes = Encoding.UTF8.GetBytes(NifNativeMutex795Names.Unknown);
        else
        {
            int length = 0;
            while (input[length] != 0) length = checked(length + 1);
            bytes = new ReadOnlySpan<byte>(input, length).ToArray();
        }
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(mutexesClosed795 || domainEnded || imageClosed528, this);
            mutexes795.EnsureCapacity(checked(mutexes795.Count + 1));
            nint pointer = Allocate(checked((nuint)IntPtr.Size + (nuint)bytes.Length + 1));
            if (pointer == 0) return 0;
            try
            {
                nint name = pointer + IntPtr.Size;
                bytes.CopyTo(new Span<byte>((void*)name, bytes.Length));
                ((byte*)name)[bytes.Length] = 0;
                var mutex = new NifNativeMutex795(this, pointer, name);
                mutexes795.Add(pointer, mutex);
                if (!mutexAddresses795.TryAdd(pointer, mutex))
                    throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);

                return pointer;
            }
            catch
            {
                mutexes795.Remove(pointer);
                Free(pointer);
                throw;
            }
        }
    }

    internal static NifNativeMutex795 ResolveMutex795(nint pointer, NifNativeAllocationOwner? expected = null)
    {
        if (pointer == 0 || !mutexAddresses795.TryGetValue(pointer, out var mutex))
            throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);
        expected ??= NifNativeCallbackScope.TryCurrent528?.Allocations;
        if (expected is not null && !ReferenceEquals(expected, mutex.Owner)
            && (expected.Arena528 is null || !ReferenceEquals(expected.Arena528, mutex.Owner.Arena528)))
            throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);

        return mutex;
    }

    internal static void DestroyMutex795(nint pointer, NifNativeAllocationOwner? expected = null)
    {
        NifNativeMutex795 mutex = ResolveMutex795(pointer, expected);
        mutex.Owner.DestroyOwnedMutex795(mutex);
    }

    private void DestroyOwnedMutex795(NifNativeMutex795 mutex)
    {
        lock (gate)
        {
            if (!mutexes795.TryGetValue(mutex.Pointer, out var actual) || !ReferenceEquals(actual, mutex))
                throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);
            mutex.Destroy();
            if (!mutexAddresses795.TryRemove(mutex.Pointer, out var removed) || !ReferenceEquals(removed, mutex))
                throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);
            mutexes795.Remove(mutex.Pointer);
            Free(mutex.Pointer);
            if (imageClosed528) TryEndDomainMembership();
        }
    }

    // Called under the existing owner gate BEFORE generic pointer reclamation.
    // Busy invalid shutdown is reported, never silently freed beneath waiters.
    private void ReclaimMutexesUnderGate795()
    {
        if (!Monitor.IsEntered(gate)) throw new InvalidOperationException(NifNativeMutex795Names.InvalidHandle);
        mutexesClosed795 = true;
        List<Exception>? failures = null;
        foreach (NifNativeMutex795 mutex in mutexes795.Values.ToArray())
            try
            {
                DestroyOwnedMutex795(mutex);
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        if (failures is not null) throw new AggregateException(failures);
    }

    internal void AbortMutexes795()
    {
        lock (gate) ReclaimMutexesUnderGate795();
    }

    private void RejectMutexAllocationMutation795(nint pointer)
    {
        if (mutexes795.ContainsKey(pointer))
            throw new InvalidOperationException(NifNativeMutex795Names.OwnedAllocation);
    }
}
