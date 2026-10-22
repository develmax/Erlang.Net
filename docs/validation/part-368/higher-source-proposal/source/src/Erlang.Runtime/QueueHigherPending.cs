namespace Erlang;

internal sealed record QueueHigherPending(Term Value, QueueHigherEmission Kind, int ReturnLine, int RunLength);
