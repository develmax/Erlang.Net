namespace Erlang;

internal static class SystemTaskFlow428
{
    internal static Task Start(Func<Task> body)
    {
        if (ExecutionContext.IsFlowSuppressed()) return Task.Run(body);
        using (ExecutionContext.SuppressFlow()) return Task.Run(body);
    }
}
