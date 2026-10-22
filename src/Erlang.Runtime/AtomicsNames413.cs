namespace Erlang;

internal static class AtomicsNames413
{
    internal const string Module = "atomics", Internal = "erts_internal", New = "atomics_new";
    internal const string Put = "put", Get = "get", Add = "add", AddGet = "add_get";
    internal const string Exchange = "exchange", CompareExchange = "compare_exchange", Info = "info";
    internal const string Size = "size", Minimum = "min", Maximum = "max", Memory = "memory";
    internal const string Foreign = "Atomics requires this runtime's exact live actor and resource domain";
    internal const string Conflict = "Atomics constructor refuses an existing or displaced binding";
    internal const string Architecture = "The authoritative atomics layout currently requires a 64-bit runtime";
    internal const string Underflow = "Atomics invocation or allocation lease underflow";
    internal const string Destructor = "Atomics allocation destructor identity is invalid";
    internal const string Identity = "Atomics requires exactly three canonical magic reference words";
    internal const int AllocationKind = 413;
    internal const ulong SignedOption = 1;
    internal const int ReferenceWords = 3;
    internal static readonly (string Function, int Arity)[] Signatures =
    [(Put, 3), (Get, 2), (Add, 3), (AddGet, 3), (Exchange, 3), (CompareExchange, 4), (Info, 1)];
}
