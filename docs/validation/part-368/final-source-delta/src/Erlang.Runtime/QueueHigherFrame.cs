namespace Erlang;

internal sealed record QueueHigherFrame(
    string Module,
    string Function,
    int Arity,
    int Line
);
