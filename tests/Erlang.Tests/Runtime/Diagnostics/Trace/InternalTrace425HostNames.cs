namespace Erlang.Tests;

internal static class InternalTrace425HostNames
{
    internal const string EventsTest = "runtime/internal-trace425-session-isolation-send-retirement";
    internal const string CallsTest = "runtime/internal-trace425-session-call-return-fanout";
    internal const string LifetimeTest = "runtime/internal-trace425-strong-weak-gc-close-ownership";
    internal const string Module = "erts_internal", Runtime = "erlang", Create = "trace_session_create", Destroy = "trace_session_destroy";
    internal const string Trace = "trace", Pattern = "trace_pattern", Info = "trace_info", SessionName = "session425";
    internal const string Undefined = "undefined", True = "true", False = "false", Flags = "flags", Send = "send", Call = "call";
    internal const string Tracer = "tracer", Receive = "receive", Message = "trace", Return = "return_from", ReturnTrace = "return_trace";
    internal const string Wildcard = "_", Abs = "abs", First = "first425", Second = "second425", BadArgument = "badarg";
    internal const string Failure = "Independent native trace session ownership or event routing differs";
    internal const string TracingOwner = "ProcessTracing", SessionDirectory = "namedSessions425";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
