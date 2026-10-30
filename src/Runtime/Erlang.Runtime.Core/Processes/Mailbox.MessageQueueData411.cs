namespace Erlang;

public sealed partial class Mailbox
{
    // Requested logical mailbox policy. CLR storage has no per-process ERTS heap;
    // this field makes no allocation/GC/native-memory/performance assertion.
    private int messageQueueData411;
    private long messageQueuePolicyRevision;
    internal long MessageQueuePolicyRevision => Volatile.Read(ref messageQueuePolicyRevision);

    internal ProcessMessageQueueDataMode411 MessageQueueData411
        => (ProcessMessageQueueDataMode411)Volatile.Read(ref messageQueueData411);

    internal ProcessMessageQueueDataMode411 ExchangeMessageQueueData411(ProcessMessageQueueDataMode411 mode)
    {
        var previous = (ProcessMessageQueueDataMode411)Interlocked.Exchange(ref messageQueueData411, (int)mode);
        if (previous != mode) Interlocked.Increment(ref messageQueuePolicyRevision);

        return previous;
    }
}
