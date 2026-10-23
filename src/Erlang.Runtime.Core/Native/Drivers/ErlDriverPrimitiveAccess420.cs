using System.Runtime.InteropServices;

namespace Erlang;

// These are the SAME real primitive delegates placed into the full callback
// vector. Queue and term code never invent a second binary allocation backend.
internal sealed unsafe class ErlDriverPrimitiveAccess420
{
    private readonly ErlDriverCallbackTypes420.driver_alloc_binary allocate;
    private readonly ErlDriverCallbackTypes420.driver_free_binary release;
    private readonly ErlDriverCallbackTypes420.driver_binary_inc_refc retain;
    private readonly ErlDriverCallbackTypes420.driver_free free;
    private readonly ErlDriverAbiLayout420 layout;

    internal ErlDriverPrimitiveAccess420(IReadOnlyDictionary<string, Delegate> callbacks, ErlDriverAbiLayout420 layout)
    {
        allocate = (ErlDriverCallbackTypes420.driver_alloc_binary)callbacks[nameof(ErlDriverCallbackTypes420.driver_alloc_binary)];
        release = (ErlDriverCallbackTypes420.driver_free_binary)callbacks[nameof(ErlDriverCallbackTypes420.driver_free_binary)];
        retain = (ErlDriverCallbackTypes420.driver_binary_inc_refc)callbacks[nameof(ErlDriverCallbackTypes420.driver_binary_inc_refc)];
        free = (ErlDriverCallbackTypes420.driver_free)callbacks[nameof(ErlDriverCallbackTypes420.driver_free)];
        this.layout = layout;
    }

    internal void Free(nint pointer) => free(pointer);

    internal void ReleaseBinary(nint binary) => release(binary);

    internal void RetainBinary(nint binary) => retain(binary);

    internal nint BinaryBytes(nint binary) => checked(binary + layout.Field(ErlDriverMemoryNames420.BinaryBytes));

    internal nuint BinarySize(nint binary) => unchecked((nuint)Marshal.ReadInt64(binary, layout.Field(ErlDriverMemoryNames420.BinarySize)));

    internal void ValidateSlice(nint binary, nuint offset, nuint size)
    {
        if (binary == nint.Zero || offset > BinarySize(binary) || size > BinarySize(binary) - offset)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
    }

    internal nint CopyToBinary(nint bytes, nuint size)
    {
        if (size != 0 && bytes == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        nint binary = allocate(size);
        if (binary == nint.Zero) throw new OutOfMemoryException();
        Buffer.MemoryCopy(
            (void*)bytes,
            (void*)BinaryBytes(binary),
            size,
            size
        );

        return binary;
    }
}
