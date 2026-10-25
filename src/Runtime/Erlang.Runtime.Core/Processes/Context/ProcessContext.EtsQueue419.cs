namespace Erlang;

public sealed partial class ProcessContext
{
    // Protected by actual runtime gate; completed tasks have no message/gift roots.
    internal LinkedList<Task> EtsQueueObservations419 { get; } = new();
}
