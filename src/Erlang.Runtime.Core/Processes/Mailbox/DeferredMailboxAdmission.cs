namespace Erlang;

internal sealed record DeferredMailboxAdmission(Term Message, bool Priority = false);
