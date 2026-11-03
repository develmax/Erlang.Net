namespace Erlang;

internal sealed unsafe partial class NifNativeAllocationOwner
{
    internal void CloseImageMutexes795(bool failedInitialization)
    {
        List<Exception>? failures = null;
        bool mutexAbortFailed = false;
        if (failedInitialization)
            try
            {
                AbortMutexes795();
            }
            catch (Exception error)
            {
                mutexAbortFailed = true;
                (failures ??= new()).Add(error);
            }
        // A busy mutex must never fall through to generic freeing. The arena
        // retains its actual pointer and a later final drain can report/retry.
        if (Arena528 is null && !mutexAbortFailed)
            try
            {
                ReclaimAll();
            }
            catch (Exception error)
            {
                (failures ??= new()).Add(error);
            }
        // Physical close is a fact even when independent reclamation failed.
        // Never leave the arena believing that the already closed image lives.
        try
        {
            ImageClosed528();
        }
        catch (Exception error)
        {
            (failures ??= new()).Add(error);
        }
        if (failures is not null) throw new AggregateException(failures);
    }
}
