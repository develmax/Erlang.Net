namespace Erlang;

internal sealed class TraceSessionState425(
    Atom name,
    long id,
    ProcessTracing tracing,
    Term tracer
)
{
    internal Atom Name { get; } = name;
    internal long Id { get; } = id;
    internal ProcessTracing Tracing { get; } = tracing;
    internal Term Tracer { get; } = tracer;
    internal LinkedListNode<TraceSessionState425>? Link { get; set; }
    internal bool Alive { get; set; } = true;
    internal WeakReference<TraceSessionHandle425>? Handle { get; set; }
    internal Term WeakTerm => Term.Tuple(Name, Term.I(Id));
}
