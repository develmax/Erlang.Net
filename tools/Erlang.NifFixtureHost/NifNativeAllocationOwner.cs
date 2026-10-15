using System.Runtime.InteropServices;

namespace Erlang.NifFixtureHost;

internal sealed unsafe class NifNativeAllocationOwner
{
    private readonly HashSet<nint> owned = new();

    internal int Count => owned.Count;

    internal nint Allocate(nuint size)
    {
        nint pointer = (nint)NativeMemory.Alloc(size == 0 ? 1 : size);
        if (pointer != 0)
            owned.Add(pointer);

        return pointer;
    }

    internal void Free(nint pointer)
    {
        if (pointer == 0)
            return;
        if (!owned.Remove(pointer))
            throw new InvalidOperationException(NifHostDiagnostics.ForeignAllocationFree);
        NativeMemory.Free((void*)pointer);
    }

    internal void ReclaimAll()
    {
        foreach (nint pointer in owned)
            NativeMemory.Free((void*)pointer);
        owned.Clear();
    }
}
