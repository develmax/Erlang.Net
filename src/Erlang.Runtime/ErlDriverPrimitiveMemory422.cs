using System.Runtime.InteropServices;
namespace Erlang;

internal sealed unsafe partial class ErlDriverPrimitiveServices422
{
    private nint Allocate(nuint length)
    {
        lock (gate)
        {
            RequireOpen();
            raw.EnsureCapacity(checked(raw.Count + 1));
            void* pointer = NativeMemory.Alloc(length);
            if (pointer is null) return nint.Zero;
            try
            {
                raw.Add((nint)pointer, length);
            }
            catch
            {
                NativeMemory.Free(pointer);
                throw;
            }
            Census();

            return (nint)pointer;
        }
    }

    private nint Reallocate(nint pointer, nuint length)
    {
        if (pointer == nint.Zero) return Allocate(length);
        lock (gate)
        {
            RequireOpen();
            if (!raw.ContainsKey(pointer)) throw new InvalidOperationException(ErlDriverPrimitiveNames422.InvalidOwner);
            raw.EnsureCapacity(checked(raw.Count + 1));
            void* moved = NativeMemory.Realloc((void*)pointer, length);
            if (moved is null && length != 0) return nint.Zero;
            raw.Remove(pointer);
            if (moved is not null) raw.Add((nint)moved, length);
            Census();

            return (nint)moved;
        }
    }

    private void Free(nint pointer)
    {
        if (pointer == nint.Zero) return;
        lock (gate)
        {
            RequireOpen();
            if (!raw.Remove(pointer)) throw new InvalidOperationException(ErlDriverPrimitiveNames422.InvalidOwner);
            NativeMemory.Free((void*)pointer);
            Census();
        }
    }

    private nint AllocateBinary(nuint length)
    {
        lock (gate)
        {
            RequireOpen();
            nuint bytes = checked((nuint)Field(ErlDriverLayoutNames422.BinaryData) + (length == 0 ? (nuint)1 : length));
            if (length > (nuint)nint.MaxValue) return nint.Zero;
            var owner = new ErlDriverBinaryAllocation422(length);
            binaries.EnsureCapacity(checked(binaries.Count + 1));
            void* pointer = NativeMemory.Alloc(bytes);
            if (pointer is null) return nint.Zero;
            try
            {
                Marshal.WriteInt64((nint)pointer, Field(ErlDriverLayoutNames422.BinaryLength), checked((long)length));
                binaries.Add((nint)pointer, owner);
                Census();

                return (nint)pointer;
            }
            catch
            {
                NativeMemory.Free(pointer);
                throw;
            }
        }
    }

    private nint ReallocateBinary(nint pointer, nuint length)
    {
        if (pointer == nint.Zero) return AllocateBinary(length);
        lock (gate)
        {
            RequireOpen();
            if (!binaries.TryGetValue(pointer, out var owner)) throw new InvalidOperationException(ErlDriverPrimitiveNames422.InvalidOwner);
            if (length > (nuint)nint.MaxValue) return nint.Zero;
            nuint bytes = checked((nuint)Field(ErlDriverLayoutNames422.BinaryData) + (length == 0 ? (nuint)1 : length));
            binaries.EnsureCapacity(checked(binaries.Count + 1));
            void* moved = NativeMemory.Realloc((void*)pointer, bytes);
            if (moved is null) return nint.Zero;
            Marshal.WriteInt64((nint)moved, Field(ErlDriverLayoutNames422.BinaryLength), checked((long)length));
            binaries.Remove(pointer);
            binaries.Add((nint)moved, owner);
            owner.Length = length;
            Census();

            return (nint)moved;
        }
    }

    private nint BinaryReferences(nint pointer, int delta)
    {
        lock (gate)
        {
            RequireOpen();
            if (!binaries.TryGetValue(pointer, out var owner)) throw new InvalidOperationException(ErlDriverPrimitiveNames422.InvalidOwner);
            long next = checked(owner.References + delta);
            if (next < 0) throw new InvalidOperationException(ErlDriverPrimitiveNames422.InvalidOwner);
            owner.References = next;

            // dec_refc deliberately does not free; source callers own release.
            return (nint)next;
        }
    }

    private void ReleaseBinary(nint pointer)
    {
        lock (gate)
        {
            RequireOpen();
            if (!binaries.TryGetValue(pointer, out var owner) || owner.References <= 0)
                throw new InvalidOperationException(ErlDriverPrimitiveNames422.InvalidOwner);
            if (--owner.References != 0) return;
            binaries.Remove(pointer);
            NativeMemory.Free((void*)pointer);
            Census();
        }
    }

    private nuint VectorToBuffer(nint vector, nint buffer, nuint length)
    {
        int count = Marshal.ReadInt32(vector, Field(ErlDriverLayoutNames422.VectorCount));
        nint vectors = Marshal.ReadIntPtr(vector, Field(ErlDriverLayoutNames422.VectorData));
        nuint copied = 0;
        for (int index = 0; index < count && copied < length; index++)
        {
            nint item = vectors + checked(index * Field(ErlDriverLayoutNames422.IoBytes));
            nuint available = unchecked((uint)Marshal.ReadInt32(item, Field(ErlDriverLayoutNames422.IoLength)));
            nuint take = Math.Min(available, length - copied);
            if (take == 0) continue;
            nint input = Marshal.ReadIntPtr(item, Field(ErlDriverLayoutNames422.IoData));
            NativeMemory.Copy((void*)input, (byte*)buffer + copied, take);
            copied += take;
        }

        return copied;
    }

    private static byte[] CString(nint pointer)
    {
        ArgumentOutOfRangeException.ThrowIfZero(pointer);
        byte* bytes = (byte*)pointer;
        nuint length = 0;
        while (bytes[length] != 0) length = checked(length + 1);
        byte[] result = new byte[checked((int)length)];
        new ReadOnlySpan<byte>(bytes, result.Length).CopyTo(result);

        return result;
    }

    private nint CopyCString(ReadOnlySpan<byte> bytes)
    {
        nint result = Allocate(checked((nuint)bytes.Length + 1));
        if (result == nint.Zero) throw new OutOfMemoryException();
        bytes.CopyTo(new Span<byte>((void*)result, bytes.Length));
        ((byte*)result)[bytes.Length] = 0;

        return result;
    }
}
