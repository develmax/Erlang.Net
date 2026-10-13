namespace Erlang;

internal sealed class ProcessInfoRequest(
    ProcessContext requester,
    ReferenceTerm id,
    IReadOnlyList<ProcessInfoItem> keys,
    bool singleton
)
{
    public ProcessContext Requester { get; } = requester;
    public ReferenceTerm Id { get; } = id;
    public IReadOnlyList<ProcessInfoItem> Keys { get; } = keys;
    public bool Singleton { get; } = singleton;
}
