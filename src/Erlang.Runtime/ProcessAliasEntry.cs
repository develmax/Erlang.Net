namespace Erlang;

internal sealed record ProcessAliasEntry(Pid Owner, bool Reply, bool Priority);
