namespace Erlang;

internal sealed class ProcessGarbageCollectionRequest(ProcessContext requester, Term id, bool major)
{
    public ProcessContext Requester { get; } = requester;
    public Term Id { get; } = id;
    public bool Major { get; } = major;
}
