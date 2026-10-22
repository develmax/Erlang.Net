namespace Erlang;

internal static class DebuggerDisabledNames413
{
    internal const string Module = "erl_debugger";
    internal const string Supported = "supported";
    internal const string Foreign = "Debugger capability belongs to this runtime's live actor";
    internal const string WrongProfile = "The debugger default-disabled profile was not selected";
    internal static readonly (string Function, int Arity)[] Signatures =
    [
        ("supported", 0),
        ("instrumentations", 0),
        ("toggle_instrumentations", 1),
        ("register", 1),
        ("unregister", 2),
        ("whereis", 0),
        ("breakpoint", 3),
        ("breakpoints", 1),
        ("breakpoints", 3),
        ("stack_frames", 2),
        ("peek_stack_frame_slot", 4),
        ("xregs_count", 1),
        ("peek_xreg", 3),
    ];
}
