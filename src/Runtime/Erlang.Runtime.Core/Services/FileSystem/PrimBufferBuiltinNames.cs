namespace Erlang;

internal static class PrimBufferBuiltinNames
{
    internal const string LockAcquired = "acquired";
    internal const string LockBusy = "busy";
    internal const string NotFound = "not_found";
    internal const string Module = "prim_buffer";
    internal const string New = "new";
    internal const string Size = "size";
    internal const string Peek = "peek_head";
    internal const string Read = "copying_read";
    internal const string Write = "write";
    internal const string Skip = "skip";
    internal const string Find = "find_byte_index";
    internal const string Lock = "try_lock";
    internal const string Unlock = "unlock";
    internal static readonly (string Name, int Arity)[] Signatures =
    [
        (New, 0), (Size, 1), (Peek, 1), (Read, 2), (Write, 2), (Skip, 2), (Find, 2), (Lock, 1), (Unlock, 1)
    ];
}
