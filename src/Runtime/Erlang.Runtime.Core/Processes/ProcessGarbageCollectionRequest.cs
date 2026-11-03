namespace Erlang;

internal sealed class ProcessGarbageCollectionRequest
{
    internal ProcessGarbageCollectionRequest(ProcessContext requester, bool major, RuntimeProcessPayload641 custody)
    {
        Requester = requester;
        Major = major;
        Custody = custody;
        QueueNode = new(this);
    }

    public ProcessContext Requester { get; }
    public Term Id => Custody.Value641;
    public bool Major { get; }
    internal RuntimeProcessPayload641 Custody { get; }
    internal LinkedListNode<ProcessGarbageCollectionRequest> QueueNode { get; }
    internal ProcessContext? Target { get; set; }
    internal ProcessCollectionRequestState State { get; set; }
}
