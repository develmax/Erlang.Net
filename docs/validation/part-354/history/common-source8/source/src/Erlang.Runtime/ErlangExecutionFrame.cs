namespace Erlang;

internal sealed record ErlangExecutionFrame(
    string Module,
    string Function,
    int Arity,
    ErlangExecutionFrame? Parent,
    bool SourceWrapper = false
);
