namespace Erlang.NifNativeBackend;

internal readonly record struct HeapBitStringVector(
    int BitLength,
    int Words32,
    nuint Header32,
    int Words64,
    nuint Header64
)
{
    internal int AllocationWords => IntPtr.Size == sizeof(ulong) ? Words64 : Words32;

    internal nuint Header => IntPtr.Size == sizeof(ulong) ? Header64 : Header32;
}
