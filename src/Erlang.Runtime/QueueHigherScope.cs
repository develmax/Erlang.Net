namespace Erlang;

internal sealed class QueueHigherScope : IDisposable
{
    private readonly List<IDisposable> scopes = new();

    internal QueueHigherScope(ProcessContext context, IReadOnlyList<QueueHigherFrame> frames)
    {
        for (int index = frames.Count - 1; index >= 0; index--)
        {
            QueueHigherFrame frame = frames[index];
            scopes.Add(context.EnterGeneratedFunction(
                frame.Module,
                frame.Function,
                frame.Arity,
                QueueHigherTrace.Location(frame.Module, frame.Line),
                QueueHigherTrace.Continuation(frame)
            ));
        }
    }

    public void Dispose()
    {
        for (int index = scopes.Count - 1; index >= 0; index--)
            scopes[index].Dispose();
    }
}
