namespace Erlang;

public sealed partial class Mailbox
{
    // Requested logical mailbox policy. CLR storage has no per-process ERTS heap;
    // this field makes no allocation/GC/native-memory/performance assertion.
    private int messageQueueData411;

    internal ProcessMessageQueueDataMode411 MessageQueueData411
        => (ProcessMessageQueueDataMode411)Volatile.Read(ref messageQueueData411);

    internal ProcessMessageQueueDataMode411 ExchangeMessageQueueData411(ProcessMessageQueueDataMode411 mode)
        => (ProcessMessageQueueDataMode411)Interlocked.Exchange(ref messageQueueData411, (int)mode);
}
