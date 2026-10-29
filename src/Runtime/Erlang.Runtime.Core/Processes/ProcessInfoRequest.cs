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
    internal ProcessInfoSnapshot601 Snapshot601 { get; } = new();
    internal MailboxGraphSnapshot593? Graph601 { get; set; }
    internal MailboxPublicationScope593? Publication601 { get; set; }
    internal bool SnapshotTransferred601 { get; set; }
    internal TaskCompletionSource Ready601 { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
