namespace Erlang.Compiler;

// Internal controls for the owned pilot. Observers run after publication and
// outside every heap/dictionary gate; the ordinary lane does not invoke them.
internal static class OwnedTrampolineCheckpoint506
{
    private static readonly AsyncLocal<Func<ProcessContext, string, ValueTask>?> observer = new();

    internal static IDisposable Enter(Func<ProcessContext, string, ValueTask> callback)
    {
        var previous = observer.Value;
        observer.Value = callback;

        return new OwnedTrampolineCheckpointScope506(() => observer.Value = previous);
    }

    internal static ValueTask Reach(ProcessContext context, string carrier)
        => observer.Value?.Invoke(context, carrier) ?? ValueTask.CompletedTask;
}
