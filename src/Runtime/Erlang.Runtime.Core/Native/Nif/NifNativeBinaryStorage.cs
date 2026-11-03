using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe partial class NifNativeBinaryStorage
{
    private static long liveBuffers;
    internal static long LiveBufferCount => Interlocked.Read(ref liveBuffers);
    private readonly object gate = new();
    private nint pointer;
    private nuint size;
    private bool nativeOwned;
    private int environmentReferences;
    internal nuint Size
    {
        get
        {
            lock (gate) return size;
        }
    }
    internal byte* Pointer
    {
        get
        {
            lock (gate)
                return pointer != 0 || nativeBorrow524 is not null ? (byte*)pointer : throw new InvalidOperationException(NifNativeBinaryNames.ExpiredStorage);
        }
    }

    private NifNativeBinaryStorage(nint pointer, nuint size, bool nativeOwned)
    {
        this.pointer = pointer;
        this.size = size;
        this.nativeOwned = nativeOwned;
        Interlocked.Increment(ref liveBuffers);
    }

    internal static NifNativeBinaryStorage? TryAllocate(nuint size, bool nativeOwned)
    {
        // An object larger than the positive address space cannot be allocated.
        if (size > (nuint)nint.MaxValue)
            return null;
        nint pointer = (nint)NativeMemory.Alloc(size == 0 ? 1 : size);
        if (pointer == 0)
            return null;
        try
        {
            return new(pointer, size, nativeOwned);
        }
        catch
        {
            NativeMemory.Free((void*)pointer);
            throw;
        }
    }


    internal BitString FreezeView(nuint offset, nuint length)
    {
        lock (gate)
        {
            if (pointer == 0 && nativeBorrow524 is null)
                throw new InvalidOperationException(NifNativeBinaryNames.ExpiredStorage);
            if (offset > size || length > size - offset)
                throw new InvalidOperationException(NifNativeBinaryNames.SubBinaryRange);
            if (length > NifNativeBinaryNames.MaximumTermBytes)
                throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);
            if (nativeBorrowView524 is { } native)
                return native.SliceBits(checked((int)offset * BitStringLayout.BitsPerByte),
                    checked((int)length * BitStringLayout.BitsPerByte));
            if (length <= NifNativeBinaryNames.SmallSubBinaryBytes)
                return new BitString(new ReadOnlySpan<byte>((byte*)pointer + offset, (int)length));
            if (size > (nuint)Array.MaxLength)
                throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);
            var parent = new ReadOnlySpan<byte>((byte*)pointer, (int)size).ToArray();

            return BitString.FromOwnedBacking(parent, (int)offset, checked((int)length * BitStringLayout.BitsPerByte));
        }
    }

    internal void RetainEnvironment()
    {
        lock (gate)
        {
            if (pointer == 0 && nativeBorrow524 is null)
                throw new InvalidOperationException(NifNativeBinaryNames.ExpiredStorage);
            environmentReferences = checked(environmentReferences + 1);
        }
    }

    internal void ReleaseEnvironment()
    {
        lock (gate)
        {
            if (environmentReferences == 0)
                throw new InvalidOperationException(NifNativeBinaryNames.ReferenceUnderflow);
            environmentReferences--;
            FreeIfUnreferenced();
        }
    }

    internal void ReleaseNativeOwnership()
    {
        lock (gate)
        {
            if (!nativeOwned || pointer == 0)
                throw new InvalidOperationException(NifNativeBinaryNames.ForeignStorage);
            nativeOwned = false;
            FreeIfUnreferenced();
        }
    }

    internal void ReleaseUnowned()
    {
        lock (gate)
            FreeIfUnreferenced();
    }

    private void FreeIfUnreferenced()
    {
        if (nativeOwned || environmentReferences != 0)
            return;
        if (ReleaseBorrowed524()) return;
        nint previous = pointer;
        pointer = 0;
        NativeMemory.Free((void*)previous);
        if (previous != 0)
            Interlocked.Decrement(ref liveBuffers);
    }

    internal bool TryReallocate(nuint length)
    {
        lock (gate)
        {
            if (!nativeOwned || pointer == 0 || environmentReferences != 0)
                throw new InvalidOperationException(NifNativeBinaryNames.ForeignStorage);
            if (length > (nuint)nint.MaxValue)
                return false;
            nint replacement = (nint)NativeMemory.Realloc((void*)pointer, length == 0 ? 1 : length);
            if (replacement == 0)
                return false;
            pointer = replacement;
            size = length;

            return true;
        }
    }
}
