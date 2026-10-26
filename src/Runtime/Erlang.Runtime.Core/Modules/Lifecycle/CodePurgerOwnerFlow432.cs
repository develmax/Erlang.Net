namespace Erlang;

internal static class CodePurgerOwnerFlow432
{
    internal static Task Start(Func<Task> body)
    {
        // This tracked owner task must not require a terminating actor scheduler
        // or retain the initiating actor's source/error-handler AsyncLocals.
        if (ExecutionContext.IsFlowSuppressed()) return Create(body);
        using (ExecutionContext.SuppressFlow()) return Create(body);
    }

    private static Task Create(Func<Task> body) => Task.Factory.StartNew(
        body,
        CancellationToken.None,
        TaskCreationOptions.DenyChildAttach,
        TaskScheduler.Default
    ).Unwrap();
}
