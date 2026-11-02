namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeWorkerCreationPolicy? workerCreation;

    private void CloseConstructorOsEnvironment(System.Collections.Concurrent.ConcurrentQueue<Exception> failures)
    {
        try
        {
            OsEnvironment?.Dispose();
        }
        catch (Exception failure)
        {
            failures.Enqueue(failure);
        }
    }
}
