using System.Runtime.InteropServices;

namespace Erlang;

// The first SysIOVec slot is reserved for the real driver's packet header.
// Payload storage is a genuine refcounted driver binary: enqv may retain it.
internal sealed unsafe class ErlDriverDistributionVector457 : IDisposable
{
    private readonly ErlDriverPrimitiveAccess420 memory;
    private nint binary;
    private nint vectors;
    private nint binaries;
    internal nint Pointer { get; private set; }

    internal ErlDriverDistributionVector457(ErlDriverPrimitiveAccess420 memory, ErlDriverAbiLayout420 layout, byte[] bytes)
    {
        this.memory = memory;
        try
        {
            int count = bytes.Length == 0 ? 1 : 2;
            int stride = layout.Field(ErlDriverMemoryNames420.SystemVectorBytes);
            Pointer = (nint)NativeMemory.AllocZeroed((nuint)layout.Field(ErlDriverMemoryNames420.IoVecBytes));
            vectors = (nint)NativeMemory.AllocZeroed(checked((nuint)(stride * count)));
            binaries = (nint)NativeMemory.AllocZeroed(checked((nuint)(IntPtr.Size * count)));
            if (Pointer == 0 || vectors == 0 || binaries == 0) throw new OutOfMemoryException();
            if (bytes.Length != 0)
            {
                fixed (byte* input = bytes) binary = memory.CopyToBinary((nint)input, (nuint)bytes.Length);
                Marshal.WriteInt32(vectors + stride, layout.Field(ErlDriverMemoryNames420.SystemVectorLength), bytes.Length);
                Marshal.WriteIntPtr(vectors + stride, layout.Field(ErlDriverMemoryNames420.SystemVectorAddress), memory.BinaryBytes(binary));
                Marshal.WriteIntPtr(binaries, IntPtr.Size, binary);
            }
            Marshal.WriteInt32(Pointer, layout.Field(ErlDriverMemoryNames420.IoVecCount), count);
            Marshal.WriteInt64(Pointer, layout.Field(ErlDriverMemoryNames420.IoVecSize), bytes.LongLength);
            Marshal.WriteIntPtr(Pointer, layout.Field(ErlDriverMemoryNames420.IoVecVectors), vectors);
            Marshal.WriteIntPtr(Pointer, layout.Field(ErlDriverMemoryNames420.IoVecBinaries), binaries);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        NativeMemory.Free((void*)Pointer);
        NativeMemory.Free((void*)vectors);
        NativeMemory.Free((void*)binaries);
        Pointer = vectors = binaries = 0;
        if (binary != 0)
        {
            nint released = binary;
            binary = 0;
            memory.ReleaseBinary(released);
        }
    }
}
