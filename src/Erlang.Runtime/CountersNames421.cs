namespace Erlang;

internal static class CountersNames421
{
    internal const string Module = "erts_internal";
    internal const string New = "counters_new", Get = "counters_get", Add = "counters_add", Put = "counters_put", Info = "counters_info";
    internal const string Size = "size", Memory = "memory";
    internal const string Foreign = "Counters requires the owning live actor and its actual normal scheduler lane";
    internal const string Architecture = "The counters native payload currently requires a 64-bit runtime";
    internal const string Underflow = "Counters invocation or allocation lease underflow";
    internal const string Destructor = "Counters allocation destructor identity is invalid";
    internal const string Identity = "Counters requires three canonical magic reference words";
    internal const int AllocationKind = 421;
    internal const int ReferenceWords = 3;
    internal const ulong CacheLineBytes = 64;
    internal const ulong CellsPerLine = CacheLineBytes / sizeof(long);
    internal static readonly (string Function, int Arity)[] Signatures =
    [(New, 1), (Get, 2), (Add, 3), (Put, 3), (Info, 1)];
}
