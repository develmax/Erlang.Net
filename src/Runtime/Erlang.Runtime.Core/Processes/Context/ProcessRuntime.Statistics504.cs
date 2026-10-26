namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Term SampleSchedulerQueues504(string key)
    {
        lock (gate)
        {
            return actorDispatcher.SampleQueues504(key, dirtyNative419);
        }
    }
}
