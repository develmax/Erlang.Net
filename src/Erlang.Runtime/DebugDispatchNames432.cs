namespace Erlang;

internal static class DebugDispatchNames432
{
    internal const string Module = "erts_debug", Instructions = "instructions", Breakpoint = "breakpoint", Wildcard = "_";
    internal const string Conflict = "Debug dispatch publication conflicts with an existing entry", Foreign = "Debug dispatch requires the exact live owning runtime";
    internal const string Arity = "Debug dispatch argument count is inconsistent", Profile = "Actual managed ExecuteCore dispatch and per-image managed controller traps; no ERTS opcode addresses or OS debugger traps";
    internal static readonly (string Function, int Arity)[] Signatures = [(Instructions, 0), (Breakpoint, 2)];
}
