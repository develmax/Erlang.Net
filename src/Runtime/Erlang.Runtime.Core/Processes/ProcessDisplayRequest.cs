namespace Erlang;

internal sealed record ProcessDisplayRequest(ProcessContext Requester, ReferenceTerm Id, Pid Target);
