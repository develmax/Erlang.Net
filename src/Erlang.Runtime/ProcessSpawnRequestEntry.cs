namespace Erlang;

internal sealed record ProcessSpawnRequestEntry(Pid Owner, Pid Child, ProcessSpawnRequestOptions Options);
