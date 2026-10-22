namespace Erlang;

internal static class DebugCodeNames427
{
    internal const string Module = "erts_debug";
    internal const string Disassemble = "disassemble";
    internal const string Size = "interpreter_size";
    internal const string Profile = "Managed register dispatcher; no native threaded BEAM opcode address range";
    internal const string Foreign = "Debug code entries require their exact owning runtime";
    internal const string Conflict = "Debug code native publication conflicts with an existing entry";
    internal const string Arity = "Debug code received an inconsistent native argument count";
    internal const string MachineMoveNext = "MoveNext";
    internal static readonly (string Function, int Arity)[] Signatures = [(Disassemble, 1), (Size, 0)];
}
