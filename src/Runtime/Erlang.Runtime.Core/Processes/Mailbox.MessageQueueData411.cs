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
        // Constructor/internal initial policy only. Public process_flag uses
        // RequestMessageQueuePolicy and its real ordered receiver marker.
        lock (gate)
        {
            var previous = MessageQueueData411;
            Volatile.Write(ref messageQueueData411, (int)mode);
            Volatile.Write(ref messageQueuePlacementMode, (int)mode);
            policyMovePending = false;
            policyMoveReady = false;
            if (previous != mode) Interlocked.Increment(ref messageQueuePolicyRevision);

            return previous;
        }
    }
}
