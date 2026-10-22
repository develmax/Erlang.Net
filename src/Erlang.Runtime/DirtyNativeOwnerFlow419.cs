namespace Erlang;

internal static class DirtyNativeOwnerFlow419
{
    internal static Task Start(Func<Task> create)
    {
        if (ExecutionContext.IsFlowSuppressed()) return create();
        using (ExecutionContext.SuppressFlow()) return create();
    }

    internal static void StartThread(Thread thread)
    {
        if (ExecutionContext.IsFlowSuppressed())
        {
            thread.Start();

            return;
        }
        using (ExecutionContext.SuppressFlow()) thread.Start();
    }
}
