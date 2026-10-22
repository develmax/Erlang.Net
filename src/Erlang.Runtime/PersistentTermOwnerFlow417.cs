namespace Erlang;

internal static class PersistentTermOwnerFlow417
{
    internal static Task Start(Func<Task> create)
    {
        if (ExecutionContext.IsFlowSuppressed()) return create();
        // No await in this scope: restore the initiating guest context immediately.
        // The permanently pending owner must not retain that guest's AsyncLocals.
        using (ExecutionContext.SuppressFlow()) return create();
    }
}
