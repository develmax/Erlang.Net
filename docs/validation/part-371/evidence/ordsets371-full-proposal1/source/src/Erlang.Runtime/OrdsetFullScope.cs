namespace Erlang;

internal sealed class OrdsetFullScope : IDisposable
{
    private readonly List<IDisposable> scopes = new();

    internal OrdsetFullScope(ProcessContext context, IReadOnlyList<OrdsetFullFrame> frames)
    {
        foreach (OrdsetFullFrame frame in frames)
            scopes.Add(OrdsetFullTrace.Enter(context, frame));
    }

    public void Dispose()
    {
        for (int index = scopes.Count - 1; index >= 0; index--)
            scopes[index].Dispose();
    }
}
