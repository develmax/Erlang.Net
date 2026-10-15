namespace Erlang;

internal static class NifRetirementFlow
{
    private static readonly AsyncLocal<Frame?> current = new();

    internal static bool IsInside(ModuleRegistry registry)
    {
        for (var frame = current.Value; frame is not null; frame = frame.Parent)
            if (ReferenceEquals(frame.Registry, registry) && !frame.Operation.Completion.IsCompleted)
                return true;

        return false;
    }

    internal static IDisposable Enter(ModuleRegistry registry, NifRetirementOperation operation)
    {
        var previous = current.Value;
        current.Value = new(registry, operation, previous);

        return new Scope(previous);
    }

    internal static Func<Task> IsolateActor(Func<Task> body) => current.Value is null ? body : WrapActor(body);

    private static Func<Task> WrapActor(Func<Task> body) => () => RunActor(body);

    private static async Task RunActor(Func<Task> body)
    {
        var previous = current.Value;
        current.Value = null;
        try
        {
            await body().ConfigureAwait(false);
        }
        finally
        {
            current.Value = previous;
        }
    }

    private sealed record Frame(ModuleRegistry Registry, NifRetirementOperation Operation, Frame? Parent);

    private sealed class Scope(Frame? previous) : IDisposable
    {
        public void Dispose() => current.Value = previous;
    }
}
