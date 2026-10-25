namespace Erlang;

internal static class DebugSharingNames425
{
    internal const string Module = "erts_debug";
    internal const string Flat = "flat_size";
    internal const string Shared = "size_shared";
    internal const string Copy = "copy_shared";
    internal const string Foreign = "Debug sharing entries require their exact owning runtime";
    internal const string Conflict = "Debug sharing native publication conflicts with an existing entry";
    internal const string Arity = "Debug sharing received an inconsistent native argument count";
    internal const string Profile = "Own immutable Term graph logical64 word protocol; not physical CLR memory";
    internal static readonly (string Function, int Arity)[] Signatures = [(Flat, 1), (Shared, 1), (Copy, 2)];
}
