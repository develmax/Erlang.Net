namespace Erlang;

internal sealed class NativeResourceTypeOpen527(
    NativeResourceType524 type,
    NativeResourceCallbackVersion527 version,
    bool created,
    LinkedListNode<NativeResourceType524>? node,
    int publicationFailureIndex
)
{
    internal int PublicationFailureIndex527 { get; } = publicationFailureIndex;
    internal NativeResourceType524 Type527 { get; } = type;
    internal NativeResourceCallbackVersion527 Version527 { get; } = version;
    internal bool Created527 { get; } = created;
    internal LinkedListNode<NativeResourceType524>? Node527 { get; } = node;
    internal NativeResourceCallbackVersion527? Previous527;
    internal Exception? RollbackVersionFailure527;
    internal Exception? RollbackPendingFailure527;
    internal Exception? RollbackTypeFailure527;
}
