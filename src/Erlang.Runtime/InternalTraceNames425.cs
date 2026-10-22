namespace Erlang;

internal static class InternalTraceNames425
{
    internal const string Module = "erts_internal", Trace = "trace", Pattern = "trace_pattern", Info = "trace_info";
    internal const string Create = "trace_session_create", Destroy = "trace_session_destroy";
    internal const string Session = "session", Any = "any", Legacy = "legacy", Default = "default";
    internal const string Undefined = "undefined", True = "true", False = "false";
    internal const string System = "system", All = "all", BadOption = "badopt", ErrorInfo = "error_info";
    internal const string Foreign = "Internal tracing requires the exact runtime and declared native signature";
    internal const string Conflict = "Internal tracing native binding conflicts with existing source or native code";
    internal const string Unsupported = "This source-valid trace feature requires additional actual event or tracer support";
    internal static readonly string[] UnsupportedFlags =
    ["all", "set_on_spawn", "procs", "set_on_first_spawn", "set_on_link", "set_on_first_link", "running", "exiting",
     "garbage_collection", "return_to", "silent", "scheduler_id", "running_ports", "running_procs", "ports"];
    internal static readonly string[] UnsupportedSessionSelectors = ["on_load", "new", "new_processes", "new_ports"];
    internal static readonly string[] UnsupportedPatterns = ["local", "meta", "call_count", "call_time", "call_memory"];
    internal const long WeakIdMaximum = (1L << 59) - 1, WeakIdMinimum = -(1L << 59);
    internal static readonly (string Function, int Arity)[] Signatures =
    [(Trace, 3), (Trace, 4), (Pattern, 3), (Pattern, 4), (Info, 3), (Create, 3), (Destroy, 1)];
}
