namespace Erlang;

internal sealed class TraceSessionHandle425(ProcessTracing root, TraceSessionState425 state)
{
    internal ProcessTracing Root { get; } = root;
    internal TraceSessionState425 State { get; } = state;

    ~TraceSessionHandle425() => Root.ReleaseTraceSession425(State);
}
