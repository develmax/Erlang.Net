using System.Runtime.InteropServices;

namespace Erlang;

// Native iov/binv arrays point into actual retained driver binaries. This is the
// queue's real externally exposed layout, not a synthetic accounting mirror.
internal sealed unsafe class ErlDriverQueueSnapshot420 : IDisposable
{
    private readonly ErlDriverPrimitiveAccess420 memory;
    private readonly ErlDriverAbiLayout420 layout;
    private readonly List<ErlDriverQueueSlice420> slices = new();
    private nint vectors;
    private nint binaries;
    private bool disposed;

    internal ErlDriverQueueSnapshot420(ErlDriverPrimitiveAccess420 memory, ErlDriverAbiLayout420 layout)
    {
        this.memory = memory;
        this.layout = layout;
    }

    internal IReadOnlyList<ErlDriverQueueSlice420> Slices => slices;
    internal nuint Size { get; private set; }
    internal nint Vectors => vectors;
    internal int Count => slices.Count;

    internal void Add(nint binary, nuint offset, nuint size)
    {
        if (size == 0) return;
        memory.ValidateSlice(binary, offset, size);
        // Windows SysIOVec has a 32-bit length even in a 64-bit process.
        while (size != 0)
        {
            nuint part = nuint.Min(size, uint.MaxValue);
            nuint total = checked(Size + part);
            var slice = new ErlDriverQueueSlice420(binary, offset, part);
            memory.RetainBinary(binary);
            try
            {
                slices.Add(slice);
            }
            catch
            {
                memory.ReleaseBinary(binary);
                throw;
            }
            Size = total;
            offset += part;
            size -= part;
        }
    }

    internal void Build()
    {
        if (slices.Count == 0) return;
        int stride = layout.Field(ErlDriverMemoryNames420.SystemVectorBytes);
        vectors = (nint)NativeMemory.AllocZeroed(checked((nuint)slices.Count * (nuint)stride));
        if (vectors == nint.Zero) throw new OutOfMemoryException();
        binaries = (nint)NativeMemory.AllocZeroed(checked((nuint)slices.Count * (nuint)IntPtr.Size));
        if (binaries == nint.Zero) throw new OutOfMemoryException();
        for (int index = 0; index < slices.Count; index++)
        {
            var slice = slices[index];
            nint vector = checked(vectors + checked(index * stride));
            Marshal.WriteInt32(vector, layout.Field(ErlDriverMemoryNames420.SystemVectorLength), unchecked((int)slice.Size));
            Marshal.WriteIntPtr(
                vector,
                layout.Field(ErlDriverMemoryNames420.SystemVectorAddress),
                checked(memory.BinaryBytes(slice.Binary) + checked((nint)slice.Offset))
            );
            Marshal.WriteIntPtr(binaries, checked(index * IntPtr.Size), slice.Binary);
        }
    }

    internal void WriteVector(nint target)
    {
        if (target == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        Marshal.WriteInt32(target, layout.Field(ErlDriverMemoryNames420.IoVecCount), Count);
        Marshal.WriteInt64(target, layout.Field(ErlDriverMemoryNames420.IoVecSize), unchecked((long)Size));
        Marshal.WriteIntPtr(target, layout.Field(ErlDriverMemoryNames420.IoVecVectors), vectors);
        Marshal.WriteIntPtr(target, layout.Field(ErlDriverMemoryNames420.IoVecBinaries), binaries);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        NativeMemory.Free((void*)vectors);
        NativeMemory.Free((void*)binaries);
        vectors = binaries = nint.Zero;
        List<Exception>? failures = null;
        foreach (var slice in slices)
        {
            try
            {
                memory.ReleaseBinary(slice.Binary);
            }
            catch (Exception failure)
            {
                (failures ??= []).Add(failure);
            }
        }
        slices.Clear();
        Size = 0;
        if (failures is not null) throw new AggregateException(failures);
    }
}
