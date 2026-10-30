using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class PhysicalNodeEntry : IDisposable
{
    private nint address;
    private GCHandle name;
    internal nint Address => Volatile.Read(ref address);

    internal PhysicalNodeEntry(LocalNodeSnapshot snapshot)
    {
        try
        {
            name = GCHandle.Alloc(snapshot.Node);
            nuint* cells = (nuint*)NativeMemory.AllocZeroed((nuint)(PhysicalNodeEntryLayout.Words * IntPtr.Size));
            if (cells == null) throw new OutOfMemoryException();
            address = (nint)cells;
            cells[PhysicalNodeEntryLayout.NameHandleWord] = (nuint)GCHandle.ToIntPtr(name);
            cells[PhysicalNodeEntryLayout.CreationWord] = snapshot.Creation;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal LocalNodeSnapshot Read()
    {
        nint value = Address;
        ObjectDisposedException.ThrowIf(value == 0, this);
        nuint* cells = (nuint*)value;
        nint handle = (nint)cells[PhysicalNodeEntryLayout.NameHandleWord];
        if (handle != GCHandle.ToIntPtr(name) || cells[PhysicalNodeEntryLayout.CreationWord] > uint.MaxValue
            || GCHandle.FromIntPtr(handle).Target is not string node)
            throw new InvalidOperationException(PhysicalNodeEntryLayout.Invalid);

        return new(node, (uint)cells[PhysicalNodeEntryLayout.CreationWord]);
    }

    internal int References
    {
        get
        {
            ObjectDisposedException.ThrowIf(Address == 0, this);

            return checked((int)((nuint*)Address)[PhysicalNodeEntryLayout.ReferencesWord]);
        }
        set
        {
            ObjectDisposedException.ThrowIf(Address == 0, this);
            if (value < 0) throw new InvalidOperationException(PhysicalNodeEntryLayout.Invalid);
            ((nuint*)Address)[PhysicalNodeEntryLayout.ReferencesWord] = (nuint)value;
        }
    }

    public void Dispose()
    {
        nint value = Interlocked.Exchange(ref address, 0);
        if (value != 0) NativeMemory.Free((void*)value);
        if (name.IsAllocated) name.Free();
    }
}
