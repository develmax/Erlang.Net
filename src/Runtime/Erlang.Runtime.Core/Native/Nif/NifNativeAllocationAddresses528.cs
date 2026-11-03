namespace Erlang;

// Real pointer authentication only. Owner ledgers/native VM retirement, not CLR
// weak references, determine lifetime. This index contains no native DLL pin.
internal static class NifNativeAllocationAddresses528
{
    internal static readonly object Gate = new();
    private static readonly Dictionary<nint, NifNativeAllocationOwner> pointers = new();

    internal static void Reserve528(int extra)
    {
        if (!Monitor.IsEntered(Gate)) throw new InvalidOperationException(NifNativeAllocationArena528Names.Gate);
        pointers.EnsureCapacity(checked(pointers.Count + extra));
    }

    internal static void Add528(nint address, NifNativeAllocationOwner owner) => pointers.Add(address, owner);

    internal static void Remove528(nint address) => pointers.Remove(address);

    internal static NifNativeAllocationOwner Resolve528(nint pointer)
    {
        lock (Gate)
            return pointers.TryGetValue(pointer, out var owner) ? owner
                : throw new InvalidOperationException(NifNativeAllocationArena528Names.Pointer);
    }
}
