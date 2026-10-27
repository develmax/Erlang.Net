namespace Erlang;

internal static class BeamOwnedCheckpoint507
{
    private static readonly AsyncLocal<Func<ProcessContext, string, ValueTask>?> observer = new();

    internal static IDisposable Enter(Func<ProcessContext, string, ValueTask> callback)
    {
        var previous = observer.Value;
        observer.Value = callback;

        return new BeamOwnedCheckpointScope507(() => observer.Value = previous);
    }

    internal static ValueTask Reach(ProcessContext context, string opcode)
        => observer.Value?.Invoke(context, opcode) ?? ValueTask.CompletedTask;
}
