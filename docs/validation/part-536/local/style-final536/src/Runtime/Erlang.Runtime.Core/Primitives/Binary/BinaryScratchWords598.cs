using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class BinaryScratchWords598 : IDisposable
{
    private nint memory;
    private readonly long count;

    internal BinaryScratchWords598(long count)
    {
        if (count <= 0 || (ulong)count > (ulong)nint.MaxValue / (ulong)sizeof(nint))
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        this.count = count;
        void* selected = NativeMemory.Alloc(checked((nuint)count * (nuint)sizeof(nint)));
        if (selected == null) throw new OutOfMemoryException();
        memory = (nint)selected;
    }

    internal nint Read598(long index) => Cell598(index)[0];

    internal void Write598(long index, nint value) => Cell598(index)[0] = value;

    private nint* Cell598(long index)
    {
        if (memory == 0) throw new ObjectDisposedException(nameof(BinaryScratchWords598));
        if ((ulong)index >= (ulong)count) throw new ArgumentOutOfRangeException(nameof(index));

        return (nint*)memory + index;
    }

    public void Dispose()
    {
        nint selected = Interlocked.Exchange(ref memory, 0);
        if (selected != 0) NativeMemory.Free((void*)selected);
    }
}
